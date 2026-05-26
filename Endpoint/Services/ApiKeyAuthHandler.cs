using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

/// <summary>
/// Simple API key authentication for cloud/Linux hosting where Windows Auth isn't available.
/// Set the "ApiKey" value in appsettings.json or environment variable ApiKey.
/// Pass it in requests via the X-Api-Key header.
/// </summary>
public class ApiKeyAuthOptions : AuthenticationSchemeOptions { }

public class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthOptions>
{
    private const string ApiKeyHeader = "X-Api-Key";

    public ApiKeyAuthHandler(
        IOptionsMonitor<ApiKeyAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredKey = Context.RequestServices
            .GetRequiredService<IConfiguration>()["ApiKey"];

        // If no API key configured, allow all requests (open testing mode)
        if (string.IsNullOrEmpty(configuredKey))
        {
            var openClaims = new[] { new Claim(ClaimTypes.Name, "test-user") };
            var openIdentity = new ClaimsIdentity(openClaims, Scheme.Name);
            var openPrincipal = new ClaimsPrincipal(openIdentity);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(openPrincipal, Scheme.Name)));
        }

        if (!Request.Headers.TryGetValue(ApiKeyHeader, out var providedKey))
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Api-Key header"));

        if (!string.Equals(configuredKey, providedKey, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));

        var claims = new[] { new Claim(ClaimTypes.Name, "api-user") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, Scheme.Name)));
    }
}
