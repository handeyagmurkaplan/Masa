namespace Masa.Api.Packs;

// Modelin döndüğü paket şekli (İrem'in schema.json'u ile aynı olmalı).
public sealed class StudyPackContent
{
    public string? Summary { get; set; }
    public List<TermItem>? Terms { get; set; }
    public List<CardItem>? Cards { get; set; }
    public List<QuestionItem>? Questions { get; set; }
    public List<DateItem>? Dates { get; set; }
}

public sealed class TermItem
{
    public string? Word { get; set; }
    public string? Definition { get; set; }
}

public sealed class CardItem
{
    public string? Front { get; set; }
    public string? Back { get; set; }
}

public sealed class QuestionItem
{
    public string? Text { get; set; }
    public List<string>? Choices { get; set; }
    public string? CorrectChoice { get; set; }
    public string? Explanation { get; set; }
}

public sealed class DateItem
{
    public string? Title { get; set; }
    public string? DueAt { get; set; }
    public string? EventType { get; set; }
}

/// <summary>Doğrulanmış paket; DatesJson için dueAt UTC'ye çevrilmiş ve boş olanlar atılmış.</summary>
public sealed record ValidPack(
    string Summary,
    IReadOnlyList<TermItem> Terms,
    IReadOnlyList<CardItem> Cards,
    IReadOnlyList<QuestionItem> Questions,
    IReadOnlyList<ValidDate> Dates);

public sealed record ValidDate(string Title, DateTime DueAt, string EventType);

public sealed record GenerateResponse(int MaterialId, int StudyPackId, string Status, string PromptVersion, bool Truncated);
