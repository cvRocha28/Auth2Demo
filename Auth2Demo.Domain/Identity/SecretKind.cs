namespace Auth2Demo.Domain.Identity;

public enum SecretKind
{
    SharedSecret = 1,
    PrivateKeyJwt = 2,
    Certificate = 3
}
