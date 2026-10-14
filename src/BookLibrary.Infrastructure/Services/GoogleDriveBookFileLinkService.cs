using System.Text.RegularExpressions;
using System.Web;
using BookLibrary.Application.Interfaces;

namespace BookLibrary.Infrastructure.Services;

public partial class GoogleDriveBookFileLinkService : IBookFileLinkService
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "drive.google.com",
        "docs.google.com"
    };

    // Google Drive file IDs are typically 25-60 alphanumeric characters, hyphens, or underscores
    [GeneratedRegex(@"^[a-zA-Z0-9_-]{15,80}$")]
    private static partial Regex FileIdFormatRegex();

    [GeneratedRegex(@"^/(?:file/d/|d/)([a-zA-Z0-9_-]{15,80})(?:/.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex FilePathRegex();

    public GoogleDriveUrlParseResult ParseAndValidateSharingUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new GoogleDriveUrlParseResult(false, null, null, "Sharing URL cannot be empty.");
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return new GoogleDriveUrlParseResult(false, null, null, "Malformed URL structure.");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return new GoogleDriveUrlParseResult(false, null, null, "Only HTTPS Google Drive URLs are permitted.");
        }

        if (!AllowedHosts.Contains(uri.Host))
        {
            return new GoogleDriveUrlParseResult(
                false,
                null,
                null,
                $"Host '{uri.Host}' is not a supported Google Drive host. Only drive.google.com and docs.google.com are allowed.");
        }

        string? extractedId = null;

        // Pattern 1: /file/d/{id}/view or /d/{id}
        var pathMatch = FilePathRegex().Match(uri.AbsolutePath);
        if (pathMatch.Success)
        {
            extractedId = pathMatch.Groups[1].Value;
        }
        // Pattern 2: /open?id={id} or /uc?id={id}
        else if (uri.AbsolutePath.Equals("/open", StringComparison.OrdinalIgnoreCase) ||
                 uri.AbsolutePath.Equals("/uc", StringComparison.OrdinalIgnoreCase))
        {
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            extractedId = queryParams["id"];
        }

        if (string.IsNullOrWhiteSpace(extractedId) || !FileIdFormatRegex().IsMatch(extractedId))
        {
            return new GoogleDriveUrlParseResult(
                false,
                null,
                null,
                "Could not extract a valid Google Drive File ID from the supplied URL.");
        }

        var normalizedUrl = BuildDirectViewUrl(extractedId);
        return new GoogleDriveUrlParseResult(true, extractedId, normalizedUrl, null);
    }

    public string BuildDownloadUrl(string googleDriveFileId)
    {
        ValidateFileIdOrThrow(googleDriveFileId);
        return $"https://drive.google.com/uc?export=download&id={Uri.EscapeDataString(googleDriveFileId)}";
    }

    public string BuildDirectViewUrl(string googleDriveFileId)
    {
        ValidateFileIdOrThrow(googleDriveFileId);
        return $"https://drive.google.com/file/d/{Uri.EscapeDataString(googleDriveFileId)}/view?usp=sharing";
    }

    private static void ValidateFileIdOrThrow(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId) || !FileIdFormatRegex().IsMatch(fileId))
        {
            throw new ArgumentException("Invalid Google Drive file identifier.", nameof(fileId));
        }
    }
}
