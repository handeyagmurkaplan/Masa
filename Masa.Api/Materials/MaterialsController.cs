using System.Data;
using Dapper;
using Masa.Api.Common;
using Masa.Api.Data;
using Masa.Api.Packs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masa.Api.Materials;

[ApiController]
[Route("api/materials")]
[Authorize]
public sealed class MaterialsController(
    SqlConnectionFactory db,
    PdfTextService pdfText,
    StudyPackGenerator generator,
    IWebHostEnvironment env) : ControllerBase
{
    public const long MaxFileBytes = 20L * 1024 * 1024;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileBytes + 1024 * 1024)]
    [ProducesResponseType<MaterialResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload([FromForm] MaterialUploadRequest request)
    {
        var file = request.File;
        var fileName = Path.GetFileName(file.FileName);
        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw ApiException.Validation("Yalnız .pdf dosyası yüklenebilir.");
        if (file.Length == 0 || file.Length > MaxFileBytes)
            throw ApiException.Validation("Dosya boş veya 20 MB'tan büyük.");

        // Dosyayı belleğe al: önce metin, metin yoksa insert etmeden 422.
        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);
        var extraction = ExtractOrReject(buffer);
        if (extraction.IsEmpty) throw ApiException.PdfEmpty();

        var userId = User.GetUserId();
        using var conn = db.Create();
        var material = await conn.QuerySingleOrDefaultAsync<MaterialRow>(
            "sp_Material_Insert",
            new
            {
                UserId = userId,
                request.CourseId,
                FileName = fileName,
                PageCount = extraction.Pages.Count,
                extraction.Truncated,
            },
            commandType: CommandType.StoredProcedure)
            ?? throw ApiException.NotFound("Ders bulunamadı.");

        // Id insert'ten sonra belli; yol storage/{userId}/{materialId}.pdf
        var path = StoragePath(userId, material.Id);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        buffer.Position = 0;
        await using (var target = System.IO.File.Create(path))
            await buffer.CopyToAsync(target);

        foreach (var page in extraction.Pages)
        {
            await conn.ExecuteAsync(
                "sp_MaterialPage_Insert",
                new { MaterialId = material.Id, page.PageNumber, page.Text },
                commandType: CommandType.StoredProcedure);
        }

        return StatusCode(StatusCodes.Status201Created, MaterialResponse.From(material));
    }

    [HttpGet]
    public async Task<IEnumerable<MaterialResponse>> ListByCourse([FromQuery] int courseId)
    {
        using var conn = db.Create();
        var rows = await conn.QueryAsync<MaterialRow>(
            "sp_Material_ListByCourse",
            new { UserId = User.GetUserId(), CourseId = courseId },
            commandType: CommandType.StoredProcedure);
        return rows.Select(MaterialResponse.From);
    }

    [HttpPost("{id:int}/generate")]
    public Task<GenerateResponse> Generate(int id) =>
        generator.GenerateAsync(User.GetUserId(), id, HttpContext.RequestAborted);

    private PdfExtraction ExtractOrReject(MemoryStream buffer)
    {
        buffer.Position = 0;
        Span<byte> header = stackalloc byte[5];
        if (buffer.Read(header) < 5 || !header.SequenceEqual("%PDF-"u8))
            throw ApiException.Validation("Dosya geçerli bir PDF değil.");

        buffer.Position = 0;
        try
        {
            return pdfText.Extract(buffer);
        }
        catch (Exception ex) when (ex is not ApiException)
        {
            throw ApiException.Validation("PDF okunamadı.");
        }
    }

    private string StoragePath(int userId, int materialId)
    {
        var root = Path.GetFullPath(Path.Combine(env.ContentRootPath, "storage"));
        var path = Path.GetFullPath(Path.Combine(root, userId.ToString(), $"{materialId}.pdf"));
        // Üst klasöre çıkan yol reddedilir.
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw ApiException.Validation("Geçersiz dosya yolu.");
        return path;
    }
}
