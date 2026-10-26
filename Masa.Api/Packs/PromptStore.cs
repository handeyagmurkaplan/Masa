using System.Text.Json.Nodes;

namespace Masa.Api.Packs;

public sealed record PromptBundle(string Version, string Instruction, JsonNode Schema, IReadOnlyList<JsonNode> Examples);

/// <summary>
/// İrem'in prompts/ klasörünü her çağrıda diskten okur; önbellek yok.
/// Prompt değişince kod değişmez, yeniden derleme gerekmez.
/// </summary>
public sealed class PromptStore(IConfiguration configuration, IHostEnvironment env)
{
    public const int MaxExamples = 2;

    public string Root => Path.GetFullPath(Path.Combine(env.ContentRootPath, configuration["Llm:PromptPath"] ?? "../prompts"));

    public async Task<PromptBundle> LoadAsync(CancellationToken ct = default)
    {
        var lines = await File.ReadAllLinesAsync(Path.Combine(Root, "studypack.md"), ct);
        var version = ParseVersion(lines.FirstOrDefault());
        var instruction = string.Join('\n', lines.Skip(1)).Trim();

        var schema = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(Root, "schema.json"), ct))
            ?? throw new InvalidOperationException("schema.json boş.");

        var examples = new List<JsonNode>();
        var examplesPath = Path.Combine(Root, "examples.json");
        if (File.Exists(examplesPath) &&
            JsonNode.Parse(await File.ReadAllTextAsync(examplesPath, ct)) is JsonArray all)
        {
            foreach (var item in all)
            {
                if (item is not JsonObject obj || obj["sendWithRequest"]?.GetValue<bool>() != true) continue;
                var copy = (JsonObject)obj.DeepClone();
                copy.Remove("sendWithRequest");
                examples.Add(copy);
                if (examples.Count == MaxExamples) break;
            }
        }

        return new PromptBundle(version, instruction, schema, examples);
    }

    /// <summary>İlk satır "version: v3" biçimindedir.</summary>
    public static string ParseVersion(string? firstLine)
    {
        const string prefix = "version:";
        if (firstLine is null || !firstLine.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("studypack.md ilk satırı 'version: vX' olmalı.");
        var version = firstLine.Trim()[prefix.Length..].Trim();
        return version.Length > 0 ? version : throw new InvalidOperationException("studypack.md sürümü boş.");
    }
}
