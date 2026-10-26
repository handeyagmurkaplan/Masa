using System.Data;
using Dapper;
using Masa.Api.Common;
using Masa.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masa.Api.Packs;

public sealed class PackRow
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public string PromptVersion { get; set; } = "";
    public string Summary { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public sealed class PackTermRow
{
    public int Id { get; set; }
    public string Word { get; set; } = "";
    public string Definition { get; set; } = "";
}

public sealed class PackCardRow
{
    public int Id { get; set; }
    public string Front { get; set; } = "";
    public string Back { get; set; } = "";
    public DateTime DueAt { get; set; }
    public string? LastResult { get; set; }
}

public sealed class PackQuestionRow
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public string ChoiceA { get; set; } = "";
    public string ChoiceB { get; set; } = "";
    public string ChoiceC { get; set; } = "";
    public string ChoiceD { get; set; } = "";
    public string CorrectChoice { get; set; } = "";
    public string Explanation { get; set; } = "";
}

public sealed record PackQuestion(int Id, string Text, IReadOnlyList<string> Choices, string CorrectChoice, string Explanation);

public sealed record PackResponse(
    int Id,
    int MaterialId,
    string PromptVersion,
    string Summary,
    DateTime CreatedAt,
    IReadOnlyList<PackTermRow> Terms,
    IReadOnlyList<PackCardRow> Cards,
    IReadOnlyList<PackQuestion> Questions);

[ApiController]
[Route("api/packs")]
[Authorize]
public sealed class PacksController(SqlConnectionFactory db) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<PackResponse> Get(int id)
    {
        using var conn = db.Create();
        using var multi = await conn.QueryMultipleAsync(
            "sp_StudyPack_Get",
            new { Id = id, UserId = User.GetUserId() },
            commandType: CommandType.StoredProcedure);

        var pack = await multi.ReadSingleOrDefaultAsync<PackRow>()
            ?? throw ApiException.NotFound("Paket bulunamadı.");
        var terms = (await multi.ReadAsync<PackTermRow>()).ToList();
        var cards = (await multi.ReadAsync<PackCardRow>()).ToList();
        var questions = (await multi.ReadAsync<PackQuestionRow>())
            .Select(q => new PackQuestion(q.Id, q.Text, [q.ChoiceA, q.ChoiceB, q.ChoiceC, q.ChoiceD], q.CorrectChoice, q.Explanation))
            .ToList();

        return new PackResponse(pack.Id, pack.MaterialId, pack.PromptVersion, pack.Summary, pack.CreatedAt, terms, cards, questions);
    }
}
