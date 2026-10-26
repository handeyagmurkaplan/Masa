using System.ComponentModel.DataAnnotations;
using System.Data;
using Dapper;
using Masa.Api.Common;
using Masa.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masa.Api.Events;

public sealed class EventRow
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = "";
    public string EventType { get; set; } = "";
    public DateTime DueAt { get; set; }
    public string Source { get; set; } = "";
    public bool IsConfirmed { get; set; }
}

public sealed record EventRequest(
    [Required] int CourseId,
    [Required, MaxLength(200)] string Title,
    [Required, RegularExpression("^(sinav|teslim)$")] string EventType,
    [Required] DateTime DueAt);

[ApiController]
[Route("api/events")]
[Authorize]
public sealed class EventsController(SqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<EventRow>> List()
    {
        using var conn = db.Create();
        return await conn.QueryAsync<EventRow>(
            "sp_Event_List",
            new { UserId = User.GetUserId() },
            commandType: CommandType.StoredProcedure);
    }

    // Source gönderilmez; prosedür "manuel" yazar.
    [HttpPost]
    [ProducesResponseType<EventRow>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(EventRequest request)
    {
        using var conn = db.Create();
        var row = await conn.QuerySingleOrDefaultAsync<EventRow>(
            "sp_Event_Insert",
            new { UserId = User.GetUserId(), request.CourseId, request.Title, request.EventType, DueAt = request.DueAt.ToUniversalTime() },
            commandType: CommandType.StoredProcedure);
        if (row is null) throw ApiException.NotFound("Ders bulunamadı.");
        return StatusCode(StatusCodes.Status201Created, row);
    }

    [HttpPut("{id:int}")]
    public async Task<EventRow> Update(int id, EventRequest request)
    {
        using var conn = db.Create();
        var row = await conn.QuerySingleOrDefaultAsync<EventRow>(
            "sp_Event_Update",
            new { Id = id, UserId = User.GetUserId(), request.CourseId, request.Title, request.EventType, DueAt = request.DueAt.ToUniversalTime() },
            commandType: CommandType.StoredProcedure);
        return row ?? throw ApiException.NotFound("Tarih bulunamadı.");
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<EventRow> Confirm(int id)
    {
        using var conn = db.Create();
        var row = await conn.QuerySingleOrDefaultAsync<EventRow>(
            "sp_Event_Confirm",
            new { Id = id, UserId = User.GetUserId() },
            commandType: CommandType.StoredProcedure);
        return row ?? throw ApiException.NotFound("Tarih bulunamadı.");
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = db.Create();
        var affected = await conn.QuerySingleOrDefaultAsync<int>(
            "sp_Event_Delete",
            new { Id = id, UserId = User.GetUserId() },
            commandType: CommandType.StoredProcedure);
        if (affected == 0) throw ApiException.NotFound("Tarih bulunamadı.");
        return NoContent();
    }
}
