namespace Masa.Api.Common;

/// <summary>Birsu'nun beklediği { error, code } gövdesine çevrilen hata.</summary>
public sealed class ApiException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;

    public static ApiException Validation(string message) => new(400, ErrorCodes.Validation, message);
    public static ApiException NotFound(string message = "Kayıt bulunamadı.") => new(404, ErrorCodes.NotFound, message);
    public static ApiException AuthInvalid() => new(401, ErrorCodes.AuthInvalid, "E-posta veya şifre hatalı.");
    public static ApiException PackInvalid(string message) => new(422, ErrorCodes.PackInvalid, message);
    public static ApiException PdfEmpty() => new(422, ErrorCodes.PdfEmpty, "PDF içinde okunabilir metin yok.");
}

public static class ErrorCodes
{
    public const string AuthInvalid = "AUTH_INVALID";
    public const string Validation = "VALIDATION";
    public const string NotFound = "NOT_FOUND";
    public const string PackInvalid = "PACK_INVALID";
    public const string PdfEmpty = "PDF_EMPTY";
    public const string Server = "SERVER_ERROR";
}

public sealed record ApiError(string Error, string Code);
