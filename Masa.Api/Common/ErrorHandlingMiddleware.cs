using Microsoft.Data.SqlClient;

namespace Masa.Api.Common;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            await Write(context, ex.Status, new ApiError(ex.Message, ex.Code));
        }
        catch (SqlException ex) when (ex.Message.Contains("EMAIL_TAKEN"))
        {
            await Write(context, 400, new ApiError("Bu e-posta zaten kayıtlı.", ErrorCodes.Validation));
        }
        catch (SqlException ex)
        {
            // Mesajda tablo adı sızmasın: ayrıntı yalnız loga.
            logger.LogError(ex, "SQL hatası");
            await Write(context, 500, new ApiError("Sunucu hatası.", ErrorCodes.Server));
        }
        catch (Exception ex) when (!context.RequestAborted.IsCancellationRequested)
        {
            logger.LogError(ex, "Beklenmeyen hata");
            await Write(context, 500, new ApiError("Sunucu hatası.", ErrorCodes.Server));
        }
    }

    private static async Task Write(HttpContext context, int status, ApiError body)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(body);
    }
}
