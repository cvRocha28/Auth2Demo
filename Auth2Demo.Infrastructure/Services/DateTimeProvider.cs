using Auth2Demo.Application.Common.Abstractions;

namespace Auth2Demo.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
