using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookLibrary.Application.Common;
using BookLibrary.Application.DTOs.Auth;
using BookLibrary.Application.DTOs.Catalog;
using BookLibrary.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BookLibrary.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegularUser_CannotAccessAdminEndpoints_Returns403Forbidden()
    {
        var client = _factory.CreateClient();

        // Register regular user
        var registerReq = new RegisterRequest("reader@example.com", "ReaderPass123!", "Regular Reader");
        var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        regResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.User.Roles.Should().ContainSingle().Which.Should().Be("User");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Attempt to create an author via Admin endpoint
        var adminResp = await client.PostAsJsonAsync("/api/v1/admin/authors",
            new UpsertAuthorRequest("Unauthorized Author", null, null, null, null, null));

        adminResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminWorkflow_CreateBook_AssociateGoogleDriveLink_Publish_AndPublicDownload_Succeeds()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // 1. Login as Bootstrapped Admin
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("admin@booklibrary.local", "AdminStrongPass123!"));
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminAuth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);

        // 2. Get seeded language
        var languages = await client.GetFromJsonAsync<List<LanguageDto>>("/api/v1/languages");
        var enLang = languages!.First(l => l.Code == "en");

        // 3. Create Author & Genre
        var authorResp = await client.PostAsJsonAsync("/api/v1/admin/authors",
            new UpsertAuthorRequest("Ursula K. Le Guin", null, "Author of Earthsea", null, null, null));
        var author = await authorResp.Content.ReadFromJsonAsync<AuthorDetailDto>();

        var genreResp = await client.PostAsJsonAsync("/api/v1/admin/genres",
            new UpsertGenreRequest("Speculative Fiction", null, "Sci-Fi and Fantasy", null));
        var genre = await genreResp.Content.ReadFromJsonAsync<GenreDetailDto>();

        // 4. Create Book (starts as Draft)
        var bookResp = await client.PostAsJsonAsync("/api/v1/admin/books",
            new UpsertBookRequest(
                "A Wizard of Earthsea",
                null,
                "A young mage named Ged learns the true name of things.",
                "0553383043",
                "9780553383041",
                null,
                1968,
                null,
                "Parnassus Press",
                enLang.Id,
                "Earthsea Cycle",
                1,
                "First Edition",
                new[] { author!.Id },
                new[] { genre!.Id }));
        bookResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var book = await bookResp.Content.ReadFromJsonAsync<BookDetailDto>();

        // 5. Associate Google Drive EPUB Link
        var fileResp = await client.PostAsJsonAsync($"/api/v1/admin/books/{book!.Id}/files",
            new AssociateBookFileRequest(
                "https://drive.google.com/file/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/view?usp=sharing",
                BookFileFormat.Epub,
                "A_Wizard_of_Earthsea.epub",
                1500000,
                enLang.Id,
                "Retail EPUB"));
        fileResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var associatedFile = await fileResp.Content.ReadFromJsonAsync<AdminBookFileDto>();
        associatedFile!.GoogleDriveFileId.Should().Be("1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms");

        // 6. Verify Draft book is NOT visible publicly yet
        var unauthClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var draftGet = await unauthClient.GetAsync($"/api/v1/books/{book.Slug}");
        draftGet.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 7. Publish Book
        var pubResp = await client.PostAsync($"/api/v1/admin/books/{book.Id}/publish", null);
        pubResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 8. Verify Public Search and Download Redirect
        var publicGet = await unauthClient.GetAsync($"/api/v1/books/{book.Slug}");
        publicGet.StatusCode.Should().Be(HttpStatusCode.OK);

        var downloadResp = await unauthClient.GetAsync($"/api/v1/books/{book.Slug}/files/{associatedFile.Id}/download");
        downloadResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        downloadResp.Headers.Location!.ToString().Should().Be("https://drive.google.com/uc?export=download&id=1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms");
    }
}
