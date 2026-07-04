using System.Threading;
using System.Threading.Tasks;

namespace Auth2Demo.Application.Common.Abstractions;

// Interface de persistência para futuras entidades próprias do domínio.
// Identity/OpenIddict ficam encapsulados no Infrastructure para não vazar EF no Application.
public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
