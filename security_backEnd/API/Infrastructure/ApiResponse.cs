using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Infrastructure;

/// <summary>
/// Unified JSON response helpers for success and error contracts.
/// </summary>
public static class ApiResponse
{
    public static IResult Success(string message, object? data = null, object? meta = null, int status = 200)
    {
        return Results.Json(new
        {
            success = true,
            message,
            data = data ?? new { },
            meta = meta ?? new { }
        }, statusCode: status);
    }

    public static IResult Error(string message, string code, object? meta = null, int status = 400)
    {
        return Results.Json(new
        {
            success = false,
            error = new
            {
                code,
                message,
                meta = meta ?? new { }
            }
        }, statusCode: status);
    }

    public static ObjectResult ErrorResult(string message, string code, object? meta = null, int status = 400)
    {
        return new ObjectResult(new
        {
            success = false,
            error = new
            {
                code,
                message,
                meta = meta ?? new { }
            }
        })
        { StatusCode = status };
    }
}
