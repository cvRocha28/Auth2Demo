namespace Auth2Demo.Domain.Identity;

public enum VerificationStatus
{
    NotStarted = 1,
    Pending = 2,
    Approved = 3,
    Rejected = 4,
    Expired = 5
}
