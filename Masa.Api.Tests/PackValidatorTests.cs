using System.Text.Json.Nodes;
using Masa.Api.Packs;

namespace Masa.Api.Tests;

public class PackValidatorTests
{
    private readonly PackValidator _validator = new();

    internal static JsonObject ValidPack(int cards = 10, int questions = 10, int terms = 5) => new()
    {
        ["summary"] = "Yigin LIFO calisir.",
        ["terms"] = new JsonArray(Enumerable.Range(1, terms)
            .Select(i => (JsonNode)new JsonObject { ["word"] = $"t{i}", ["definition"] = $"d{i}" }).ToArray()),
        ["cards"] = new JsonArray(Enumerable.Range(1, cards)
            .Select(i => (JsonNode)new JsonObject { ["front"] = $"on{i}", ["back"] = $"arka{i}" }).ToArray()),
        ["questions"] = new JsonArray(Enumerable.Range(1, questions)
            .Select(i => (JsonNode)new JsonObject
            {
                ["text"] = $"soru{i}",
                ["choices"] = new JsonArray("a", "b", "c", "d"),
                ["correctChoice"] = "B",
                ["explanation"] = "cunku",
            }).ToArray()),
        ["dates"] = new JsonArray(
            new JsonObject { ["title"] = "Vize", ["dueAt"] = "2026-11-05T09:00:00Z", ["eventType"] = "sinav" },
            new JsonObject { ["title"] = "Odev", ["dueAt"] = "", ["eventType"] = "teslim" }),
    };

    [Fact]
    public void Valid_pack_passes_and_drops_dates_without_dueAt()
    {
        var result = _validator.Validate(ValidPack().ToJsonString());
        Assert.True(result.IsValid, result.Error);
        var date = Assert.Single(result.Pack!.Dates);
        Assert.Equal(DateTimeKind.Utc, date.DueAt.Kind);
        Assert.Equal(new DateTime(2026, 11, 5, 9, 0, 0, DateTimeKind.Utc), date.DueAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(11)]
    public void Card_count_must_be_exactly_ten(int count) =>
        Assert.False(_validator.Validate(ValidPack(cards: count).ToJsonString()).IsValid);

    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    public void Question_count_must_be_exactly_ten(int count) =>
        Assert.False(_validator.Validate(ValidPack(questions: count).ToJsonString()).IsValid);

    [Theory]
    [InlineData(4)]
    [InlineData(9)]
    public void Term_count_outside_five_to_eight_fails(int count) =>
        Assert.False(_validator.Validate(ValidPack(terms: count).ToJsonString()).IsValid);

    [Fact]
    public void Broken_json_fails() =>
        Assert.False(_validator.Validate("{ summary: ").IsValid);

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_summary_fails(string? summary)
    {
        var pack = ValidPack();
        pack["summary"] = summary;
        Assert.False(_validator.Validate(pack.ToJsonString()).IsValid);
    }

    [Fact]
    public void Summary_longer_than_1200_fails()
    {
        var pack = ValidPack();
        pack["summary"] = new string('x', 1201);
        Assert.False(_validator.Validate(pack.ToJsonString()).IsValid);
        pack["summary"] = new string('x', 1200);
        Assert.True(_validator.Validate(pack.ToJsonString()).IsValid);
    }

    [Fact]
    public void Three_choices_fails()
    {
        var pack = ValidPack();
        pack["questions"]![0]!["choices"] = new JsonArray("a", "b", "c");
        Assert.False(_validator.Validate(pack.ToJsonString()).IsValid);
    }

    [Fact]
    public void Correct_choice_E_fails()
    {
        var pack = ValidPack();
        pack["questions"]![3]!["correctChoice"] = "E";
        Assert.False(_validator.Validate(pack.ToJsonString()).IsValid);
    }

    [Fact]
    public void Empty_explanation_fails()
    {
        var pack = ValidPack();
        pack["questions"]![0]!["explanation"] = " ";
        Assert.False(_validator.Validate(pack.ToJsonString()).IsValid);
    }

    [Fact]
    public void Unknown_event_type_fails()
    {
        var pack = ValidPack();
        pack["dates"]![0]!["eventType"] = "proje";
        Assert.False(_validator.Validate(pack.ToJsonString()).IsValid);
    }
}
