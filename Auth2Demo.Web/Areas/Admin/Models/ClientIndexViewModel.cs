namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class ClientIndexViewModel
{
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
    public string ConsentType { get; set; } = string.Empty;
    public IReadOnlyList<string> GrantTypes { get; set; } = Array.Empty<string>();
}
