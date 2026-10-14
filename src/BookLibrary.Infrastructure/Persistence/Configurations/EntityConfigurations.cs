using BookLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookLibrary.Infrastructure.Persistence.Configurations;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(10);
        builder.Property(x => x.NativeName).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(220);
        builder.Property(x => x.Biography).HasMaxLength(4000);
        builder.Property(x => x.PortraitImageUrl).HasMaxLength(500);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.DisplayName);
    }
}

public class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(140);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => x.Slug).IsUnique();

        builder.HasOne(x => x.ParentGenre)
            .WithMany(x => x.SubGenres)
            .HasForeignKey(x => x.ParentGenreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(320);
        builder.Property(x => x.Synopsis).IsRequired().HasMaxLength(5000);
        builder.Property(x => x.Isbn10).HasMaxLength(13);
        builder.Property(x => x.Isbn13).HasMaxLength(17);
        builder.Property(x => x.CoverImageUrl).HasMaxLength(500);
        builder.Property(x => x.Publisher).HasMaxLength(200);
        builder.Property(x => x.SeriesName).HasMaxLength(200);
        builder.Property(x => x.SeriesOrder).HasPrecision(6, 2);
        builder.Property(x => x.Edition).HasMaxLength(100);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.Title);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PublicationYear);
        builder.HasIndex(x => x.CreatedAtUtc);

        builder.HasOne(x => x.OriginalLanguage)
            .WithMany(x => x.OriginalLanguageBooks)
            .HasForeignKey(x => x.OriginalLanguageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BookAuthorConfiguration : IEntityTypeConfiguration<BookAuthor>
{
    public void Configure(EntityTypeBuilder<BookAuthor> builder)
    {
        builder.HasKey(x => new { x.BookId, x.AuthorId });

        builder.HasOne(x => x.Book)
            .WithMany(x => x.BookAuthors)
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Author)
            .WithMany(x => x.BookAuthors)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BookGenreConfiguration : IEntityTypeConfiguration<BookGenre>
{
    public void Configure(EntityTypeBuilder<BookGenre> builder)
    {
        builder.HasKey(x => new { x.BookId, x.GenreId });

        builder.HasOne(x => x.Book)
            .WithMany(x => x.BookGenres)
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Genre)
            .WithMany(x => x.BookGenres)
            .HasForeignKey(x => x.GenreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BookFileConfiguration : IEntityTypeConfiguration<BookFile>
{
    public void Configure(EntityTypeBuilder<BookFile> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GoogleDriveFileId).IsRequired().HasMaxLength(128);
        builder.Property(x => x.OriginalSharingUrl).HasMaxLength(1000);
        builder.Property(x => x.OriginalFileName).HasMaxLength(260);
        builder.Property(x => x.EditionNote).HasMaxLength(200);

        builder.HasIndex(x => new { x.BookId, x.GoogleDriveFileId }).IsUnique();
        builder.HasIndex(x => new { x.BookId, x.Format, x.IsActive });

        builder.HasOne(x => x.Book)
            .WithMany(x => x.BookFiles)
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Language)
            .WithMany(x => x.BookFiles)
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BookRequestConfiguration : IEntityTypeConfiguration<BookRequest>
{
    public void Configure(EntityTypeBuilder<BookRequest> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BookTitle).IsRequired().HasMaxLength(300);
        builder.Property(x => x.AuthorName).HasMaxLength(200);
        builder.Property(x => x.Language).HasMaxLength(50);
        builder.Property(x => x.IsbnOrEdition).HasMaxLength(100);
        builder.Property(x => x.AdditionalDetails).HasMaxLength(1500);
        builder.Property(x => x.RequesterId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.UserVisibleResponse).HasMaxLength(1000);
        builder.Property(x => x.AdminNotes).HasMaxLength(1500);

        builder.HasIndex(x => x.RequesterId);
        builder.HasIndex(x => x.Status);
    }
}

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.AdminNotes).HasMaxLength(1500);
        builder.HasIndex(x => x.Status);
    }
}

public class UserBookmarkConfiguration : IEntityTypeConfiguration<UserBookmark>
{
    public void Configure(EntityTypeBuilder<UserBookmark> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        builder.HasIndex(x => new { x.UserId, x.BookId }).IsUnique();

        builder.HasOne(x => x.Book)
            .WithMany(x => x.Bookmarks)
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(256);
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}

public class BrokenLinkReportConfiguration : IEntityTypeConfiguration<BrokenLinkReport>
{
    public void Configure(EntityTypeBuilder<BrokenLinkReport> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ReporterUserId).HasMaxLength(450);

        builder.HasOne(x => x.BookFile)
            .WithMany()
            .HasForeignKey(x => x.BookFileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorUserId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EntityId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Details).HasMaxLength(2000);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
