using System.Globalization;

namespace CliniSys.Api.Middleware;

/// <summary>
/// Sets <see cref="CultureInfo.CurrentCulture"/> and <see cref="CultureInfo.CurrentUICulture"/>
/// from the <c>Accept-Language</c> request header before the MVC pipeline runs.
/// Falls back to <c>en-US</c> for unsupported locales.
/// </summary>
public class LocalizationMiddleware
{
    private static readonly string[] Supported = ["en-US", "pt-BR", "es-ES"];
    private readonly RequestDelegate _next;

    /// <summary>Initialises the middleware.</summary>
    /// <param name="next">Next middleware in the pipeline.</param>
    public LocalizationMiddleware(RequestDelegate next) => _next = next;

    /// <summary>Resolves the locale and sets thread culture, then continues.</summary>
    /// <param name="context">HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var header = context.Request.Headers.AcceptLanguage.FirstOrDefault() ?? "en-US";
        var locale = Supported.FirstOrDefault(s => header.Contains(s)) ?? "en-US";
        var culture = new CultureInfo(locale);
        CultureInfo.CurrentCulture   = culture;
        CultureInfo.CurrentUICulture = culture;
        await _next(context);
    }
}
