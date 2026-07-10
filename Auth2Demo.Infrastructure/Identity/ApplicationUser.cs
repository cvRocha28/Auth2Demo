using Auth2Demo.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace Auth2Demo.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? CompanyId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? Locale { get; set; } = "en-US";
    public string? Language { get; set; } = "en";
    public string? Culture { get; set; } = "en-US";
    public string? Country { get; set; }
    public string? TimeZone { get; set; } = "E. South America Standard Time";
    public UserStatus Status { get; set; } = UserStatus.Pending;
    public VerificationStatus DocumentVerificationStatus { get; set; } = VerificationStatus.NotStarted;
    public VerificationStatus FaceVerificationStatus { get; set; } = VerificationStatus.NotStarted;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? LastPasswordChangeAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
