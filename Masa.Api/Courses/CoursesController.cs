using System.ComponentModel.DataAnnotations;
using System.Data;
using Dapper;
using Masa.Api.Common;
using Masa.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masa.Api.Courses;

public sealed class CourseRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public string? Color { get; set; }
}

public sealed record CourseRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(20)] string? Code,
    [RegularExpression("^#[0-9A-Fa-f]{6}$")] string? Color);

[ApiController]
[Route("api/courses")]
[Authorize]
public sealed class CoursesController(SqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<CourseRow>> List()
    {
        using var conn = db.Create();
        return await conn.QueryAsync<CourseRow>(
            "sp_Course_List",
            new { UserId = User.GetUserId() },
            commandType: CommandType.StoredProcedure);
    }

    [HttpPost]
    [ProducesResponseType<CourseRow>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CourseRequest request)
    {
        using var conn = db.Create();
        var row = await conn.QuerySingleAsync<CourseRow>(
            "sp_Course_Insert",
            new { UserId = User.GetUserId(), request.Name, request.Code, request.Color },
            commandType: CommandType.StoredProcedure);
        return StatusCode(StatusCodes.Status201Created, row);
    }

    [HttpPut("{id:int}")]
    public async Task<CourseRow> Update(int id, CourseRequest request)
    {
        using var conn = db.Create();
        var row = await conn.QuerySingleOrDefaultAsync<CourseRow>(
            "sp_Course_Update",
            new { Id = id, UserId = User.GetUserId(), request.Name, request.Code, request.Color },
            commandType: CommandType.StoredProcedure);
        return row ?? throw ApiException.NotFound("Ders bulunamadı.");
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = db.Create();
        var affected = await conn.QuerySingleOrDefaultAsync<int>(
            "sp_Course_Delete",
            new { Id = id, UserId = User.GetUserId() },
            commandType: CommandType.StoredProcedure);
        if (affected == 0) throw ApiException.NotFound("Ders bulunamadı.");
        return NoContent();
    }
}
