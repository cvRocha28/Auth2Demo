using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Auth2Demo.Web.Security;

public sealed class SecurityHeadersMiddleware
{
    private const string AuthorizationEndpoint = "/connect/authorize";

    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IWebHostEnvironment environment,
        IOptions<SecurityHeadersOptions> options)
    {
        _next = next;
        _environment = environment;
        _options = options.Value;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IOpenIddictApplicationManager applicationManager)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "accelerometer=(), autoplay=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin-allow-popups";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";

        var registeredClientFormAction = await ResolveRegisteredClientFormActionAsync(
            context,
            applicationManager,
            context.RequestAborted);

        var formActionSources = _options.Csp.FormActionSources
            .Concat(registeredClientFormAction is null ? [] : [registeredClientFormAction]);

        var formAction = BuildSourceDirective(
            "form-action",
            ["'self'"],
            formActionSources);

        var frameSources = BuildSourceDirective(
            "frame-src",
            ["'self'"],
            _options.Csp.FrameSources);

        // OpenIddict uses an auto-submitting HTML form when response_mode=form_post.
        // The generated protocol response contains a small inline script. Rather than
        // weakening the whole site, inline execution is allowed only for this exact
        // authorization endpoint and only when the registered client requested form_post.
        var scriptSources = await IsFormPostAuthorizationRequestAsync(context)
            ? "script-src 'self' 'unsafe-inline'"
            : "script-src 'self'";

        var connectSources = _environment.IsDevelopment()
            ? "connect-src 'self' http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*"
            : "connect-src 'self'";

        var policy = string.Join("; ", new[]
        {
            "default-src 'self'",
            "base-uri 'self'",
            "object-src 'none'",
            "frame-ancestors 'self'",
            formAction,
            scriptSources,
            "style-src 'self' https://cdn.jsdelivr.net",
            "font-src 'self' https://cdn.jsdelivr.net data:",
            "img-src 'self' data: https:",
            connectSources,
            frameSources,
            "manifest-src 'self'",
            "media-src 'self'",
            "worker-src 'self' blob:"
        });

        if (!_environment.IsDevelopment())
        {
            policy += "; upgrade-insecure-requests";
        }

        headers["Content-Security-Policy"] = policy;

        await _next(context);
    }

    private static async Task<bool> IsFormPostAuthorizationRequestAsync(HttpContext context)
    {
        if (!string.Equals(context.Request.Path.Value, AuthorizationEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var responseMode = await GetAuthorizationParameterAsync(
            context,
            OpenIddictConstants.Parameters.ResponseMode);

        return string.Equals(
            responseMode,
            OpenIddictConstants.ResponseModes.FormPost,
            StringComparison.Ordinal);
    }

    private static async Task<string?> ResolveRegisteredClientFormActionAsync(
        HttpContext context,
        IOpenIddictApplicationManager applicationManager,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(context.Request.Path.Value, AuthorizationEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // The first authorization request is a GET, but the access-denied screen
        // posts the original OIDC parameters back in the form body. Reading only
        // Request.Query here caused the CSP to block OpenIddict's form_post response,
        // leaving the browser on a blank /connect/authorize page.
        var clientId = await GetAuthorizationParameterAsync(
            context,
            OpenIddictConstants.Parameters.ClientId);
        var requestedRedirectUri = await GetAuthorizationParameterAsync(
            context,
            OpenIddictConstants.Parameters.RedirectUri);

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(requestedRedirectUri))
        {
            return null;
        }

        if (!Uri.TryCreate(requestedRedirectUri, UriKind.Absolute, out var requestedUri))
        {
            return null;
        }

        var application = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application is null)
        {
            return null;
        }

        var registeredRedirectUris = await applicationManager.GetRedirectUrisAsync(application, cancellationToken);
        var isRegistered = registeredRedirectUris.Any(registeredRedirectUri =>
            Uri.TryCreate(registeredRedirectUri, UriKind.Absolute, out var registeredUri) &&
            Uri.Compare(
                registeredUri,
                requestedUri,
                UriComponents.AbsoluteUri,
                UriFormat.SafeUnescaped,
                StringComparison.Ordinal) == 0);

        if (!isRegistered)
        {
            return null;
        }

        if (requestedUri.Scheme != Uri.UriSchemeHttps &&
            !(requestedUri.Scheme == Uri.UriSchemeHttp && requestedUri.IsLoopback))
        {
            return null;
        }

        return requestedUri.GetLeftPart(UriPartial.Authority);
    }

    private static async Task<string?> GetAuthorizationParameterAsync(
        HttpContext context,
        string parameterName)
    {
        if (context.Request.Query.TryGetValue(parameterName, out var queryValue) &&
            !string.IsNullOrWhiteSpace(queryValue.ToString()))
        {
            return queryValue.ToString();
        }

        if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.HasFormContentType)
        {
            return null;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        return form.TryGetValue(parameterName, out var formValue)
            ? formValue.ToString()
            : null;
    }

    private static string BuildSourceDirective(
        string directive,
        IEnumerable<string> requiredSources,
        IEnumerable<string>? configuredSources)
    {
        var sources = requiredSources
            .Concat(configuredSources ?? [])
            .Where(static source => !string.IsNullOrWhiteSpace(source))
            .Select(static source => source.Trim())
            .Where(IsSafeCspSource)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return $"{directive} {string.Join(' ', sources)}";
    }

    private static bool IsSafeCspSource(string source)
    {
        if (source is "'self'" or "'none'")
        {
            return true;
        }

        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            uri.AbsolutePath != "/")
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps ||
               (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
    }
}

public static class SecurityHeadersApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAuth2DemoSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
