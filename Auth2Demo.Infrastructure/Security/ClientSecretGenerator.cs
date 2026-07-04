using System.Security.Cryptography;

namespace Auth2Demo.Infrastructure.Security;

public interface IClientSecretGenerator
{
    string GenerateSecret(int bytes = 48);
}

public sealed class ClientSecretGenerator : IClientSecretGenerator
{
    public string GenerateSecret(int bytes = 48)
    {
        var buffer = RandomNumberGenerator.GetBytes(bytes);
        return Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
