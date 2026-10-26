using System.Globalization;
using System.Text.Json;

namespace Masa.Api.Packs;

public sealed record PackValidationResult(ValidPack? Pack, string? Error)
{
    public bool IsValid => Pack is not null;
}

/// <summary>
/// Gemini'nin structured output'undan sonra ikinci katman. Fazlayı kırpmaz; kural dışıysa geçersiz.
/// </summary>
public sealed class PackValidator
{
    public const int SummaryMaxLength = 1200;
    public const int TermsMin = 5, TermsMax = 8;
    public const int CardCount = 10, QuestionCount = 10;
    private static readonly string[] ChoiceLetters = ["A", "B", "C", "D"];
    private static readonly string[] EventTypes = ["sinav", "teslim"];

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PackValidationResult Validate(string rawJson)
    {
        StudyPackContent? content;
        try
        {
            content = JsonSerializer.Deserialize<StudyPackContent>(rawJson, JsonOptions);
        }
        catch (JsonException)
        {
            return Fail("Model cevabı JSON değil.");
        }
        if (content is null) return Fail("Model cevabı boş.");

        if (string.IsNullOrWhiteSpace(content.Summary) || content.Summary.Length > SummaryMaxLength)
            return Fail($"Özet boş veya {SummaryMaxLength} karakterden uzun.");

        var terms = content.Terms ?? [];
        if (terms.Count is < TermsMin or > TermsMax)
            return Fail($"Terim sayısı {TermsMin}-{TermsMax} arası olmalı ({terms.Count}).");
        if (terms.Any(t => IsBlank(t.Word) || IsBlank(t.Definition)))
            return Fail("Terimde kelime veya tanım boş.");

        var cards = content.Cards ?? [];
        if (cards.Count != CardCount)
            return Fail($"Kart sayısı {CardCount} olmalı ({cards.Count}).");
        if (cards.Any(c => IsBlank(c.Front) || IsBlank(c.Back)))
            return Fail("Kartta ön veya arka yüz boş.");

        var questions = content.Questions ?? [];
        if (questions.Count != QuestionCount)
            return Fail($"Soru sayısı {QuestionCount} olmalı ({questions.Count}).");
        foreach (var q in questions)
        {
            if (IsBlank(q.Text)) return Fail("Soru metni boş.");
            if (q.Choices is not { Count: 4 } || q.Choices.Any(IsBlank)) return Fail("Her soruda dört dolu şık olmalı.");
            if (!ChoiceLetters.Contains(q.CorrectChoice)) return Fail("Doğru şık A, B, C veya D olmalı.");
            if (IsBlank(q.Explanation)) return Fail("Soru açıklaması boş.");
        }

        var dates = new List<ValidDate>();
        foreach (var d in content.Dates ?? [])
        {
            if (!EventTypes.Contains(d.EventType)) return Fail("Tarih türü yalnız sinav veya teslim olabilir.");
            if (IsBlank(d.DueAt)) continue; // dueAt boşsa DatesJson'a girmez.
            if (!DateTime.TryParse(d.DueAt, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dueAt))
                return Fail("Tarih okunamadı.");
            if (IsBlank(d.Title)) return Fail("Tarih başlığı boş.");
            dates.Add(new ValidDate(d.Title!.Trim(), dueAt, d.EventType!));
        }

        return new PackValidationResult(new ValidPack(content.Summary.Trim(), terms, cards, questions, dates), null);
    }

    private static bool IsBlank(string? s) => string.IsNullOrWhiteSpace(s);
    private static PackValidationResult Fail(string error) => new(null, error);
}
