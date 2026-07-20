using System.Net;
using System.Text.Json;
using CliniSys.Application.Common.Exceptions;
using FluentValidation;

namespace CliniSys.Api.Middleware;

/// <summary>
/// Global exception-handling middleware. Maps known exception types to consistent
/// JSON responses and logs unhandled errors server-side.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    /// <summary>Initialises the middleware.</summary>
    /// <param name="next">Next middleware in the pipeline.</param>
    /// <param name="logger">Logger for unhandled exceptions.</param>
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next; _logger = logger;
    }

    /// <summary>Catches exceptions and writes a JSON error response.</summary>
    /// <param name="context">HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (ValidationException ex)
        {
            await WriteAsync(context, HttpStatusCode.BadRequest, "Validation failed.",
                ex.Errors.Select(e => e.ErrorMessage));
        }
        catch (NotFoundException ex)  { await WriteAsync(context, HttpStatusCode.NotFound,    ex.Message); }
        catch (ConflictException ex)  { await WriteAsync(context, HttpStatusCode.Conflict,    ex.Message); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");
            await WriteAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteAsync(
        HttpContext ctx, HttpStatusCode code, string message, IEnumerable<string>? errors = null)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode  = (int)code;
        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var body = errors is not null ? (object)new { message, errors } : new { message };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body, opts));
    }
}
