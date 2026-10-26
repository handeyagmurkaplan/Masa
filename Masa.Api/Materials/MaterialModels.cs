using System.ComponentModel.DataAnnotations;

namespace Masa.Api.Materials;

public static class MaterialStatus
{
    public const string Waiting = "bekliyor";
    public const string Preparing = "hazirlaniyor";
    public const string Ready = "hazir";
    public const string Failed = "hatali";
}

public sealed class MaterialRow
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string FileName { get; set; } = "";
    public string Status { get; set; } = "";
    public int PageCount { get; set; }
    public bool Truncated { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class MaterialPageRow
{
    public int PageNumber { get; set; }
    public string Text { get; set; } = "";
}

public sealed class MaterialUploadRequest
{
    [Required] public int CourseId { get; set; }
    [Required] public IFormFile File { get; set; } = null!;
}

public sealed record MaterialResponse(int Id, string FileName, string Status, int PageCount, bool Truncated)
{
    public static MaterialResponse From(MaterialRow row) =>
        new(row.Id, row.FileName, row.Status, row.PageCount, row.Truncated);
}
