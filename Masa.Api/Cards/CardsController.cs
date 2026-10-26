using System.ComponentModel.DataAnnotations;
using System.Data;
using Dapper;
using Masa.Api.Common;
using Masa.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masa.Api.Cards;

public sealed record ReviewRequest([Required, RegularExpression("^(again|good)$")] string Result);

public sealed class CardReviewRow
{
    public int Id { get; set; }
    public DateTime DueAt { get; set; }
    public string LastResult { get; set; } = "";
}

[ApiController]
[Route("api/cards")]
[Authorize]
public sealed class CardsController(SqlConnectionFactory db) : ControllerBase
{
    // C# yalnız result'ı doğrular; +1 / +3 gün sp_Card_Review içinde.
    [HttpPost("{id:int}/review")]
    public async Task<CardReviewRow> Review(int id, ReviewRequest request)
    {
        using var conn = db.Create();
        var row = await conn.QuerySingleOrDefaultAsync<CardReviewRow>(
            "sp_Card_Review",
            new { Id = id, UserId = User.GetUserId(), request.Result },
            commandType: CommandType.StoredProcedure);
        return row ?? throw ApiException.NotFound("Kart bulunamadı.");
    }
}
