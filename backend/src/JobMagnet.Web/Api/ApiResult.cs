namespace JobMagnet.Web.Api;

// Expected failures (bad input, wrong password, missing profile) are values, not exceptions.
public record ApiResult<T>(T? Value, string? Error, int StatusCode)
{
    public bool IsSuccess => Error is null;
}

public static class ApiResult
{
    public static ApiResult<T> Success<T>(T? value, int statusCode) => new(value, null, statusCode);

    public static ApiResult<T> Failure<T>(string error, int statusCode) => new(default, error, statusCode);
}
