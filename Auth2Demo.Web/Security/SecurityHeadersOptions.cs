namespace Auth2Demo.Web.Security;

public sealed class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    public CspOptions Csp { get; init; } = new();

    public sealed class CspOptions
    {
        public string[] FormActionSources { get; init; } =
        [
            "https://accounts.google.com",
            "https://login.microsoftonline.com",
            "https://login.live.com"
        ];

        public string[] FrameSources { get; init; } =
        [
            "https://accounts.google.com",
            "https://login.microsoftonline.com",
            "https://login.live.com"
        ];
    }
}
