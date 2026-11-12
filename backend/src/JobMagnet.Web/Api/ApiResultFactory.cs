namespace JobMagnet.Web.Api;

public static class ApiResult
{
    public static ApiResult<T> Success<T>(T? value, int statusCode) => new(value, null, statusCode);

    public static ApiResult<T> Failure<T>(string error, int statusCode) => new(default, error, statusCode);
}
