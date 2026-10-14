using BookLibrary.Application.DTOs.Catalog;
using BookLibrary.Application.Exceptions;
using BookLibrary.Domain.Entities;
using BookLibrary.Domain.Enums;
using BookLibrary.Infrastructure.Persistence;
using BookLibrary.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BookLibrary.Tests.Unit.Services;

public class BookServiceUnitTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SearchPublishedBooks_ExcludesDraftAndArchivedBooks()
    {
        using var db = CreateInMemoryDb();
        var lang = new Language { Name = "English", Code = "en" };
        var author = new Author { DisplayName = "J.R.R. Tolkien", Slug = "j-r-r-tolkien" };
        var genre = new Genre { Name = "Fantasy", Slug = "fantasy" };
        db.Languages.Add(lang);
        db.Authors.Add(author);
        db.Genres.Add(genre);
        await db.SaveChangesAsync();

        var sut = new BookService(db, new GoogleDriveBookFileLinkService());

        var draft = await sut.CreateBookAsync(new UpsertBookRequest(
            "Unpublished Draft Book", null, "Synopsis", null, null, null, 2024, null, null,
            lang.Id, null, null, null, new[] { author.Id }, new[] { genre.Id }), "admin-1");

        var published = await sut.CreateBookAsync(new UpsertBookRequest(
            "The Hobbit", null, "Fantasy classic", null, "9780261102217", null, 1937, null, null,
            lang.Id, null, null, null, new[] { author.Id }, new[] { genre.Id }), "admin-1");
        await sut.PublishBookAsync(published.Id, "admin-1");

        var archived = await sut.CreateBookAsync(new UpsertBookRequest(
            "Archived Book", null, "Old synopsis", null, null, null, 1990, null, null,
            lang.Id, null, null, null, new[] { author.Id }, new[] { genre.Id }), "admin-1");
        await sut.ArchiveBookAsync(archived.Id, "admin-1");

        var searchResult = await sut.SearchPublishedBooksAsync(new BookSearchQuery());

        searchResult.TotalCount.Should().Be(1);
        searchResult.Items.Single().Title.Should().Be("The Hobbit");

        // Verify draft book is not retrievable via public endpoint
        Func<Task> getDraft = () => sut.GetPublishedBookByIdOrSlugAsync(draft.Slug);
        await getDraft.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssociateBookFile_PreventsDuplicateGoogleDriveFileIdPerBook()
    {
        using var db = CreateInMemoryDb();
        var lang = new Language { Name = "English", Code = "en" };
        var author = new Author { DisplayName = "Frank Herbert", Slug = "frank-herbert" };
        var genre = new Genre { Name = "Sci-Fi", Slug = "sci-fi" };
        db.Languages.Add(lang);
        db.Authors.Add(author);
        db.Genres.Add(genre);
        await db.SaveChangesAsync();

        var sut = new BookService(db, new GoogleDriveBookFileLinkService());
        var book = await sut.CreateBookAsync(new UpsertBookRequest(
            "Dune", null, "Arrakis epic", null, null, null, 1965, null, null,
            lang.Id, null, null, null, new[] { author.Id }, new[] { genre.Id }), "admin-1");

        var driveUrl = "https://drive.google.com/file/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/view?usp=sharing";
        await sut.AssociateBookFileAsync(book.Id, new AssociateBookFileRequest(
            driveUrl, BookFileFormat.Epub, "Dune.epub", 2048000, lang.Id, "First Edition"), "admin-1");

        Func<Task> duplicateAdd = () => sut.AssociateBookFileAsync(book.Id, new AssociateBookFileRequest(
            driveUrl, BookFileFormat.Pdf, "Dune.pdf", 4096000, lang.Id, null), "admin-1");

        await duplicateAdd.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task BookRequestService_EnforcesOwnershipAndPreventsIdor()
    {
        using var db = CreateInMemoryDb();
        var sut = new BookRequestService(db);

        var created = await sut.CreateRequestAsync("user-alice", new BookLibrary.Application.DTOs.Requests.CreateBookRequestDto(
            "The Silmarillion", "J.R.R. Tolkien", RequestedBookFormat.Epub, "en", null, null));

        Func<Task> bobAccess = () => sut.GetMyRequestByIdAsync("user-bob", created.Id);
        await bobAccess.Should().ThrowAsync<ForbiddenException>();

        Func<Task> bobCancel = () => sut.CancelMyRequestAsync("user-bob", created.Id);
        await bobCancel.Should().ThrowAsync<ForbiddenException>();
    }
}
