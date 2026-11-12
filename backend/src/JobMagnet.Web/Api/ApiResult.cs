namespace JobMagnet.Web.Api;

// Expected failures (bad input, wrong password, missing profile) are values, not exceptions.
public record ApiResult<T>(T? Value, string? Error, int StatusCode)
{
    public bool IsSuccess => Error is null;
}
