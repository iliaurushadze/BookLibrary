namespace BookLibrary.Application.Interfaces;

public record GoogleDriveUrlParseResult(
    bool IsValid,
    string? FileId,
    string? NormalizedSharingUrl,
    string? ErrorMessage);

public interface IBookFileLinkService
{
    GoogleDriveUrlParseResult ParseAndValidateSharingUrl(string url);
    string BuildDownloadUrl(string googleDriveFileId);
    string BuildDirectViewUrl(string googleDriveFileId);
}
