using System.Data;
using Dapper;
using Masa.Api.Common;
using Masa.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masa.Api.Today;

public sealed class TodayEventRow
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = "";
    public string Title { get; set; } = "";
    public string EventType { get; set; } = "";
    public DateTime DueAt { get; set; }
    public int DaysLeft { get; set; }
}

public sealed class TodayCardRow
{
    public int Id { get; set; }
    public int StudyPackId { get; set; }
    public string CourseName { get; set; } = "";
    public string Front { get; set; } = "";
    public string Back { get; set; } = "";
    public DateTime DueAt { get; set; }
}

public sealed record TodayResponse(IReadOnlyList<TodayEventRow> Events, IReadOnlyList<TodayCardRow> Cards);

[ApiController]
[Route("api/today")]
[Authorize]
public sealed class TodayController(SqlConnectionFactory db) : ControllerBase
{
    // Sıra, TOP 20, onay filtresi ve daysLeft prosedürde; burada hesap yok.
    [HttpGet]
    public async Task<TodayResponse> Get()
    {
        using var conn = db.Create();
        using var multi = await conn.QueryMultipleAsync(
            "sp_Today_List",
            new { UserId = User.GetUserId() },
            commandType: CommandType.StoredProcedure);
        var events = (await multi.ReadAsync<TodayEventRow>()).ToList();
        var cards = (await multi.ReadAsync<TodayCardRow>()).ToList();
        return new TodayResponse(events, cards);
    }
}
