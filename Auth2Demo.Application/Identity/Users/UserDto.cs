using Auth2Demo.Domain.Identity;
using System;

namespace Auth2Demo.Application.Identity.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string UserName,
    string DisplayName,
    UserStatus Status,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);
