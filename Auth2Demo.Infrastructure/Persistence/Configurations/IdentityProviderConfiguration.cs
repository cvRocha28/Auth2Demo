using Auth2Demo.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth2Demo.Infrastructure.Persistence.Configurations;

public sealed class IdentityProviderConfiguration : IEntityTypeConfiguration<IdentityProvider>
{
    public void Configure(EntityTypeBuilder<IdentityProvider> builder)
    {
        builder.ToTable("IdentityProviders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Scheme)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.IconCssClass).HasMaxLength(120);
        builder.Property(x => x.ButtonText).HasMaxLength(160);
        builder.Property(x => x.ClientId).HasMaxLength(400);
        builder.Property(x => x.ClientSecret).HasMaxLength(1000);
        builder.Property(x => x.Authority).HasMaxLength(500);
        builder.Property(x => x.CallbackPath).HasMaxLength(200);

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Scheme).IsUnique();
        builder.HasIndex(x => new { x.IsEnabled, x.SortOrder });
    }
}
