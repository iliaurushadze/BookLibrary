using BookLibrary.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace BookLibrary.Tests.Unit.Services;

public class GoogleDriveBookFileLinkServiceTests
{
    private readonly GoogleDriveBookFileLinkService _sut = new();

    [Theory]
    [InlineData("https://drive.google.com/file/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/view?usp=sharing", "1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms")]
    [InlineData("https://drive.google.com/file/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit", "1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms")]
    [InlineData("https://drive.google.com/open?id=1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms", "1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms")]
    [InlineData("https://drive.google.com/uc?id=1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms&export=download", "1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms")]
    [InlineData("https://docs.google.com/file/d/1A2B3C4D5E6F7G8H9I0J_k-lMnOpQrStUv/view", "1A2B3C4D5E6F7G8H9I0J_k-lMnOpQrStUv")]
    public void ParseAndValidateSharingUrl_ValidGoogleDriveUrls_ExtractsFileIdCorrectly(string url, string expectedFileId)
    {
        var result = _sut.ParseAndValidateSharingUrl(url);

        result.IsValid.Should().BeTrue();
        result.FileId.Should().Be(expectedFileId);
        result.ErrorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("http://drive.google.com/file/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/view")] // HTTP not HTTPS
    [InlineData("https://evil-phishing.com/file/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/view")] // Disallowed host
    [InlineData("https://drive.google.com.attacker.com/file/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/view")] // SSRF subdomain trick
    [InlineData("https://169.254.169.254/latest/meta-data/")] // SSRF metadata IP
    [InlineData("https://drive.google.com/file/d/short/view")] // Too short ID
    [InlineData("https://drive.google.com/file/d/invalid$char$id1234567890/view")] // Invalid characters
    public void ParseAndValidateSharingUrl_InvalidOrMaliciousUrls_RejectsUrl(string invalidUrl)
    {
        var result = _sut.ParseAndValidateSharingUrl(invalidUrl);

        result.IsValid.Should().BeFalse();
        result.FileId.Should().BeNull();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void BuildDownloadUrl_ValidFileId_ReturnsCanonicalDownloadLink()
    {
        var fileId = "1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms";
        var downloadUrl = _sut.BuildDownloadUrl(fileId);

        downloadUrl.Should().Be("https://drive.google.com/uc?export=download&id=1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms");
    }
}
