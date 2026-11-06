using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters;

/// <summary>
/// Adds Cache-Control: no-store on authenticated / auth responses.
/// </summary>
public sealed class NoStoreResultFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        context.HttpContext.Response.Headers.Pragma = "no-cache";
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
