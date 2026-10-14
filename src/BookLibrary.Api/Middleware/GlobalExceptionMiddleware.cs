using BookLibrary.Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BookLibrary.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? context.TraceIdentifier;
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        var (statusCode, title, detail, errors) = exception switch
        {
            ValidationException vex => (
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                "One or more validation errors occurred.",
                vex.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ),
            BadRequestException brex => (StatusCodes.Status400BadRequest, "Bad Request", brex.Message, null),
            NotFoundException nfex => (StatusCodes.Status404NotFound, "Not Found", nfex.Message, null),
            ConflictException cfex => (StatusCodes.Status409Conflict, "Conflict", cfex.Message, null),
            ForbiddenException fbex => (StatusCodes.Status403Forbidden, "Forbidden", fbex.Message, null),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred. Please contact support with the correlation ID.", null)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for TraceId {CorrelationId}", correlationId);
        }
        else
        {
            _logger.LogWarning("Handled domain/validation exception ({StatusCode}) for TraceId {CorrelationId}: {Message}",
                statusCode, correlationId, exception.Message);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["correlationId"] = correlationId;
        if (errors != null)
        {
            problem.Extensions["errors"] = errors;
        }

        await context.Response.WriteAsJsonAsync(problem);
    }
}
