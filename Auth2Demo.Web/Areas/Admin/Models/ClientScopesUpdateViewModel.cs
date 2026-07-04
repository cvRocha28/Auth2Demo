namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class ClientScopesUpdateViewModel
{
    public string ClientId { get; set; } = string.Empty;
    public List<string> SelectedScopes { get; set; } = new();
}
