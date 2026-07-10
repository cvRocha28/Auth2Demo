using Auth2Demo.Application.Services.Admin;

namespace Auth2Demo.Infrastructure.Services.Admin;

public sealed class EnterpriseApplicationService : IEnterpriseApplicationService
{
    private readonly IEnterpriseApplicationRepository _repository;

    public EnterpriseApplicationService(IEnterpriseApplicationRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<EnterpriseApplicationListItemData>> ListAsync()
    {
        return _repository.ListAsync();
    }

    public Task<EnterpriseApplicationEditData?> GetForEditAsync(Guid applicationId)
    {
        return _repository.GetForEditAsync(applicationId);
    }

    public Task SaveAsync(SaveEnterpriseApplicationData model)
    {
        return _repository.SaveAsync(model);
    }
}
