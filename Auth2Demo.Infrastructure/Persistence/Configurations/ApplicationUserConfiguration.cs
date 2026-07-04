using Auth2Demo.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth2Demo.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("IdentityUsers");
        builder.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.AvatarUrl).HasMaxLength(600);
        builder.Property(x => x.Department).HasMaxLength(120);
        builder.Property(x => x.JobTitle).HasMaxLength(120);
        builder.Property(x => x.Locale).HasMaxLength(20);
        builder.Property(x => x.TimeZone).HasMaxLength(80);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.DocumentVerificationStatus).HasConversion<int>();
        builder.Property(x => x.FaceVerificationStatus).HasConversion<int>();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
