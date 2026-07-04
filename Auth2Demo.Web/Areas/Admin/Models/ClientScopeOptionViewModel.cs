namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class ClientScopeOptionViewModel
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAllowed { get; set; }
}
