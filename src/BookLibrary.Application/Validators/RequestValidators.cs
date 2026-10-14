using BookLibrary.Application.DTOs.Auth;
using BookLibrary.Application.DTOs.Catalog;
using BookLibrary.Application.DTOs.Requests;
using FluentValidation;

namespace BookLibrary.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.DisplayName).NotEmpty().MinimumLength(2).MaximumLength(100);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class UpsertBookRequestValidator : AbstractValidator<UpsertBookRequest>
{
    public UpsertBookRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Synopsis).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Isbn10).MaximumLength(13).When(x => !string.IsNullOrEmpty(x.Isbn10));
        RuleFor(x => x.Isbn13).MaximumLength(17).When(x => !string.IsNullOrEmpty(x.Isbn13));
        RuleFor(x => x.OriginalLanguageId).NotEmpty();
        RuleFor(x => x.AuthorIds).NotEmpty().WithMessage("At least one author is required.");
        RuleFor(x => x.GenreIds).NotEmpty().WithMessage("At least one genre is required.");
        RuleFor(x => x.PublicationYear).InclusiveBetween(1000, 2100).When(x => x.PublicationYear.HasValue);
    }
}

public class AssociateBookFileRequestValidator : AbstractValidator<AssociateBookFileRequest>
{
    public AssociateBookFileRequestValidator()
    {
        RuleFor(x => x.SharingUrl).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Format).IsInEnum();
        RuleFor(x => x.OriginalFileName).MaximumLength(260);
        RuleFor(x => x.FileSizeBytes).GreaterThan(0).When(x => x.FileSizeBytes.HasValue);
    }
}

public class UpsertAuthorRequestValidator : AbstractValidator<UpsertAuthorRequest>
{
    public UpsertAuthorRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Biography).MaximumLength(4000);
    }
}

public class UpsertGenreRequestValidator : AbstractValidator<UpsertGenreRequest>
{
    public UpsertGenreRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class UpsertLanguageRequestValidator : AbstractValidator<UpsertLanguageRequest>
{
    public UpsertLanguageRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().Length(2, 10);
        RuleFor(x => x.NativeName).MaximumLength(100);
    }
}

public class CreateBookRequestDtoValidator : AbstractValidator<CreateBookRequestDto>
{
    public CreateBookRequestDtoValidator()
    {
        RuleFor(x => x.BookTitle).NotEmpty().MaximumLength(300);
        RuleFor(x => x.AuthorName).MaximumLength(200);
        RuleFor(x => x.PreferredFormat).IsInEnum();
        RuleFor(x => x.Language).MaximumLength(50);
        RuleFor(x => x.IsbnOrEdition).MaximumLength(100);
        RuleFor(x => x.AdditionalDetails).MaximumLength(1500);
    }
}

public class CreateContactMessageDtoValidator : AbstractValidator<CreateContactMessageDto>
{
    public CreateContactMessageDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MinimumLength(10).MaximumLength(4000);
    }
}
