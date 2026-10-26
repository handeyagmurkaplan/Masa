using System.Data;
using System.Text;
using System.Text.Json;
using Dapper;
using Masa.Api.Common;
using Masa.Api.Data;
using Masa.Api.Materials;

namespace Masa.Api.Packs;

/// <summary>
/// Malzeme sayfaları → İrem'in promptu → Gemini → PackValidator → sp_StudyPack_Save.
/// Geçersiz cevapta Save çağrılmaz; tabloya yarım paket yazılmaz.
/// </summary>
public sealed class StudyPackGenerator(
    SqlConnectionFactory db,
    PromptStore prompts,
    IStudyPackModel model,
    PackValidator validator,
    IHostEnvironment env,
    ILogger<StudyPackGenerator> logger)
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(90);
    private static readonly JsonSerializerOptions CamelCase = new(JsonSerializerDefaults.Web);

    // İstemci bağlantıyı kesse de iş 90 sn içinde biter ve durum "hazirlaniyor"da kalmaz;
    // bu yüzden istek iptali bilinçli olarak dinlenmez.
    public async Task<GenerateResponse> GenerateAsync(int userId, int materialId, CancellationToken _)
    {
        using var conn = db.Create();
        var material = await conn.QuerySingleOrDefaultAsync<MaterialRow>(
            "sp_Material_Get",
            new { Id = materialId, UserId = userId },
            commandType: CommandType.StoredProcedure)
            ?? throw ApiException.NotFound("Malzeme bulunamadı.");

        if (material.Status == MaterialStatus.Preparing)
            throw ApiException.Validation("Paket zaten hazırlanıyor.");

        await SetStatus(conn, userId, materialId, MaterialStatus.Preparing, null);

        string raw;
        PromptBundle bundle;
        try
        {
            using var timeout = new CancellationTokenSource(Timeout);
            var pages = await conn.QueryAsync<MaterialPageRow>(
                "sp_MaterialPage_List",
                new { MaterialId = materialId, UserId = userId },
                commandType: CommandType.StoredProcedure);

            bundle = await prompts.LoadAsync(timeout.Token);
            raw = await model.GenerateJsonAsync(BuildPrompt(bundle, pages), bundle.Schema, timeout.Token);
        }
        catch (OperationCanceledException)
        {
            await SetStatus(conn, userId, materialId, MaterialStatus.Failed, "Paket zamanında hazırlanamadı.");
            throw ApiException.PackInvalid("Paket zamanında hazırlanamadı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Paket üretimi başarısız (material {MaterialId})", materialId);
            await SetStatus(conn, userId, materialId, MaterialStatus.Failed, "Model cevap vermedi.");
            throw ApiException.PackInvalid("Paket hazırlanamadı, model cevap vermedi.");
        }

        var result = validator.Validate(raw);
        if (!result.IsValid)
        {
            await WriteRawLog(materialId, raw, result.Error!);
            await SetStatus(conn, userId, materialId, MaterialStatus.Failed, Shorten(result.Error!));
            throw ApiException.PackInvalid(result.Error!);
        }

        var pack = result.Pack!;
        // Transaction prosedürün içinde; burada tek çağrı.
        var studyPackId = await conn.QuerySingleAsync<int>(
            "sp_StudyPack_Save",
            new
            {
                MaterialId = materialId,
                UserId = userId,
                PromptVersion = bundle.Version,
                pack.Summary,
                TermsJson = JsonSerializer.Serialize(pack.Terms, CamelCase),
                CardsJson = JsonSerializer.Serialize(pack.Cards, CamelCase),
                QuestionsJson = JsonSerializer.Serialize(pack.Questions, CamelCase),
                DatesJson = JsonSerializer.Serialize(pack.Dates, CamelCase),
            },
            commandType: CommandType.StoredProcedure);

        return new GenerateResponse(materialId, studyPackId, MaterialStatus.Ready, bundle.Version, material.Truncated);
    }

    /// <summary>Talimat İrem'in dosyasından birebir; buraya cümle eklenmez, yalnız parçalar birleştirilir.</summary>
    public static string BuildPrompt(PromptBundle bundle, IEnumerable<MaterialPageRow> pages)
    {
        var sb = new StringBuilder(bundle.Instruction).Append("\n\n");
        foreach (var example in bundle.Examples)
            sb.Append(example.ToJsonString()).Append("\n\n");
        foreach (var page in pages.OrderBy(p => p.PageNumber))
            sb.Append(page.Text).Append('\n');
        return sb.ToString();
    }

    private static Task SetStatus(IDbConnection conn, int userId, int materialId, string status, string? message) =>
        conn.ExecuteAsync(
            "sp_Material_SetStatus",
            new { Id = materialId, UserId = userId, Status = status, ErrorMessage = message },
            commandType: CommandType.StoredProcedure);

    private async Task WriteRawLog(int materialId, string raw, string error)
    {
        try
        {
            var dir = Path.Combine(env.ContentRootPath, "logs");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"pack-{materialId}-{DateTime.UtcNow:yyyyMMddHHmmss}.json");
            await File.WriteAllTextAsync(file, $"// {error}\n{raw}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ham model cevabı loglanamadı");
        }
    }

    private static string Shorten(string s) => s.Length <= 200 ? s : s[..200];
}
