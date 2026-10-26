using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Masa.Api.Packs;

public interface IStudyPackModel
{
    /// <summary>Modelin ham JSON cevabını döner; doğrulama PackValidator'da.</summary>
    Task<string> GenerateJsonAsync(string prompt, JsonNode schema, CancellationToken ct);
}

/// <summary>Gemini generateContent REST çağrısı; yapılandırılmış çıktı schema.json ile.</summary>
public sealed class GeminiClient(HttpClient http, IConfiguration configuration, IHostEnvironment env) : IStudyPackModel
{
    public async Task<string> GenerateJsonAsync(string prompt, JsonNode schema, CancellationToken ct)
    {
        // Demo/test: Development'ta sahte cevap dosyası verilirse model çağrılmaz.
        var fake = configuration["Llm:FakeResponseFile"];
        if (env.IsDevelopment() && !string.IsNullOrWhiteSpace(fake))
            return await File.ReadAllTextAsync(Path.GetFullPath(Path.Combine(env.ContentRootPath, fake)), ct);

        var apiKey = configuration["Llm:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Llm:ApiKey user-secrets içinde tanımlı değil.");
        var model = configuration["Llm:Model"] ?? "gemini-3.8-flash";

        var body = new JsonObject
        {
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray { new JsonObject { ["text"] = prompt } },
                },
            },
            ["generationConfig"] = new JsonObject
            {
                ["responseMimeType"] = "application/json",
                ["responseJsonSchema"] = schema.DeepClone(),
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{Uri.EscapeDataString(model)}:generateContent")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("x-goog-api-key", apiKey);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Gemini {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}");

        var json = await response.Content.ReadFromJsonAsync<JsonNode>(ct);
        return json?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>() ?? "";
    }
}
