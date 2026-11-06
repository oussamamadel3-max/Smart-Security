using API.Infrastructure;
using Domain.Exceptions;

namespace API.Middleware;

/// <summary>
/// Maps unhandled exceptions to the unified ApiResponse.Error contract.
/// </summary>
public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        var (status, code, message) = ex switch
        {
            UnauthenticatedUserException u => (StatusCodes.Status401Unauthorized, u.ErrorCode, u.Message),
            ForbiddenException f => (StatusCodes.Status403Forbidden, f.ErrorCode, f.Message),
            ConflictException c => (StatusCodes.Status409Conflict, c.ErrorCode, c.Message),
            NotFoundException n => (StatusCodes.Status404NotFound, n.ErrorCode, n.Message),
            BadRequestException b => (StatusCodes.Status400BadRequest, b.ErrorCode, b.Message),
            ServiceUnavailableException s => (StatusCodes.Status503ServiceUnavailable, s.ErrorCode, s.Message),
            BusinessException biz => (StatusCodes.Status400BadRequest, biz.ErrorCode, biz.Message),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "حدث خطأ غير متوقع.")
        };

        if (status >= 500)
            _logger.LogError(ex, "Unhandled exception");
        else
            _logger.LogWarning(ex, "Handled business exception {Code}", code);

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            success = false,
            error = new
            {
                code,
                message,
                meta = new { }
            }
        };

        await context.Response.WriteAsJsonAsync(payload);
    }
}

public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ErrorHandlingMiddleware>();
}
