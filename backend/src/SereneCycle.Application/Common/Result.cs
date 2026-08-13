namespace SereneCycle.Application.Common;

/// <summary>
/// Servis katmanının dönüş tipi. Beklenen hatalar (yanlış şifre, süresi
/// dolmuş kod) exception yerine buradan taşınır; exception'lar gerçekten
/// beklenmedik durumlar için ayrılır.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, ErrorCode errorCode, string? error)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public ErrorCode ErrorCode { get; }
    public string? Error { get; }

    public static Result Success() => new(true, ErrorCode.None, null);

    public static Result Failure(ErrorCode code, string error) =>
        new(false, code, error);
}

public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, ErrorCode errorCode, string? error)
        : base(isSuccess, errorCode, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) =>
        new(true, value, ErrorCode.None, null);

    public static new Result<T> Failure(ErrorCode code, string error) =>
        new(false, default, code, error);
}

/// <summary>
/// API katmanının doğru HTTP durum kodunu seçebilmesi için hata sınıfı.
/// </summary>
public enum ErrorCode
{
    None,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden
}
