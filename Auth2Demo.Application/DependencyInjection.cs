using Microsoft.Extensions.DependencyInjection;

namespace Auth2Demo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
