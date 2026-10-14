namespace BookLibrary.Domain.Entities;

public class BrokenLinkReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookFileId { get; set; }
    public BookFile? BookFile { get; set; }
    public string? ReporterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsResolved { get; set; } = false;
    public DateTime ReportedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
}
