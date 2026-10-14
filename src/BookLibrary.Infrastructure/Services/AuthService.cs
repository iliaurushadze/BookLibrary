using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BookLibrary.Application.Common;
using BookLibrary.Application.DTOs.Auth;
using BookLibrary.Application.Exceptions;
using BookLibrary.Application.Interfaces;
using BookLibrary.Domain.Constants;
using BookLibrary.Domain.Entities;
using BookLibrary.Infrastructure.Identity;
using BookLibrary.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BookLibrary.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwtOptions;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        IOptions<JwtOptions> jwtOptions,
        IEmailService emailService)
    {
        _userManager = userManager;
        _db = db;
        _jwtOptions = jwtOptions.Value;
        _emailService = emailService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (existingUser != null)
        {
            throw new ConflictException("An account with this email address already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new BadRequestException(errors);
        }

        // Public registration NEVER grants Admin privileges
        await _userManager.AddToRoleAsync(user, RoleNames.User);

        var verificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _emailService.SendEmailAsync(
            user.Email!,
            "Verify your Digital Book Library account",
            $"Your email verification token for user {user.Id}: {verificationToken}",
            ct);

        return await GenerateAuthTokensAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user == null)
        {
            throw new BadRequestException("Invalid email or password.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new ForbiddenException("Account is temporarily locked due to multiple failed login attempts. Please try again later.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            throw new BadRequestException("Invalid email or password.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return await GenerateAuthTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var tokenHash = ComputeSha256Hash(request.RefreshToken);
        var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (storedToken == null || !storedToken.IsActive)
        {
            throw new BadRequestException("Invalid or expired refresh token.");
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user == null)
        {
            throw new BadRequestException("User account no longer exists.");
        }

        // Rotate refresh token
        storedToken.RevokedAtUtc = DateTime.UtcNow;
        var authResponse = await GenerateAuthTokensAsync(user, ct);
        storedToken.ReplacedByTokenHash = ComputeSha256Hash(authResponse.RefreshToken);
        await _db.SaveChangesAsync(ct);

        return authResponse;
    }

    public async Task LogoutAsync(string userId, string? refreshToken, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var hash = ComputeSha256Hash(refreshToken);
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == userId && t.TokenHash == hash, ct);
            if (token != null && token.RevokedAtUtc == null)
            {
                token.RevokedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        else
        {
            var activeTokens = await _db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
                .ToListAsync(ct);
            foreach (var token in activeTokens)
            {
                token.RevokedAtUtc = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<UserProfileDto> GetProfileAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User was not found.");

        var roles = await _userManager.GetRolesAsync(user);
        return new UserProfileDto(user.Id, user.Email!, user.DisplayName, user.EmailConfirmed, roles.ToList(), user.CreatedAtUtc);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User was not found.");

        user.DisplayName = request.DisplayName.Trim();
        await _userManager.UpdateAsync(user);

        return await GetProfileAsync(userId, ct);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User was not found.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user == null)
        {
            // Return silently to prevent account enumeration
            return;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        await _emailService.SendEmailAsync(
            user.Email!,
            "Password Reset Request",
            $"Use this password reset token: {token}",
            ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user == null)
        {
            throw new BadRequestException("Password reset failed. Invalid token or email.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new BadRequestException("Password reset failed. Invalid token or password requirements not met.");
        }
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            throw new BadRequestException("Invalid email verification request.");

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
            throw new BadRequestException("Email verification failed.");
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await _userManager.Users.CountAsync(ct);
        var users = await _userManager.Users
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = new List<AdminUserDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var locked = await _userManager.IsLockedOutAsync(u);
            dtos.Add(new AdminUserDto(u.Id, u.Email!, u.DisplayName, u.EmailConfirmed, locked, roles.ToList()));
        }

        return new PagedResult<AdminUserDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task UpdateUserRoleAsync(string targetUserId, string roleName, string actorUserId, CancellationToken ct = default)
    {
        if (roleName != RoleNames.Admin && roleName != RoleNames.User)
            throw new BadRequestException($"Unsupported role '{roleName}'. Allowed values: {RoleNames.User}, {RoleNames.Admin}.");

        var user = await _userManager.FindByIdAsync(targetUserId)
            ?? throw new NotFoundException($"User '{targetUserId}' was not found.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, roleName);

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = "UpdateUserRole",
            EntityType = nameof(ApplicationUser),
            EntityId = targetUserId,
            Details = $"Set role to {roleName}",
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task<AuthResponse> GenerateAuthTokensAsync(ApplicationUser user, CancellationToken ct)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.Email, user.Email!),
            new("display_name", user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = ComputeSha256Hash(rawRefreshToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync(ct);

        var profile = new UserProfileDto(user.Id, user.Email!, user.DisplayName, user.EmailConfirmed, roles.ToList(), user.CreatedAtUtc);
        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            rawRefreshToken,
            profile);
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
