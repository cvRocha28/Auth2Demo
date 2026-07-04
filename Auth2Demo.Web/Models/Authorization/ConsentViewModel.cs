namespace Auth2Demo.Web.Models.Authorization;

public sealed class ConsentViewModel
{
    public string ApplicationName { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
}
