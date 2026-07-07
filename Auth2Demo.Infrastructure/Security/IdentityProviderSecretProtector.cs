using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Auth2Demo.Infrastructure.Security;

public interface IIdentityProviderSecretProtector
{
    string? Protect(string? clientSecret);
    string? Unprotect(string? protectedClientSecret);
    bool IsProtected(string? value);
}

public sealed class IdentityProviderSecretProtector : IIdentityProviderSecretProtector
{
    private const string Purpose = "Auth2Demo.IdentityProviders.ClientSecret.v1";

    private readonly IDataProtector _protector;

    public IdentityProviderSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string? Protect(string? clientSecret)
    {
        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            return null;
        }

        var trimmedSecret = clientSecret.Trim();

        // Keeps already protected values untouched. This also helps safe re-saves and legacy migrations.
        if (IsProtected(trimmedSecret))
        {
            return trimmedSecret;
        }

        return _protector.Protect(trimmedSecret);
    }

    public string? Unprotect(string? protectedClientSecret)
    {
        if (string.IsNullOrWhiteSpace(protectedClientSecret))
        {
            return null;
        }

        var trimmedSecret = protectedClientSecret.Trim();

        try
        {
            return _protector.Unprotect(trimmedSecret);
        }
        catch (CryptographicException)
        {
            // Backward compatibility: secrets saved before this change were stored as plain text.
            // They remain usable and will be encrypted the next time the provider is saved with a new secret.
            return trimmedSecret;
        }
    }

    public bool IsProtected(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            _protector.Unprotect(value.Trim());
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
