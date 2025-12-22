using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public sealed class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
{
  public void Configure(EntityTypeBuilder<UserRefreshToken> b)
  {
    b.ToTable("user_refresh_tokens");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
    b.Property(x => x.UserId).HasColumnName("user_id");
    b.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsFixedLength();
    b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
    b.Property(x => x.RevokedAt).HasColumnName("revoked_at");
    b.Property(x => x.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash").HasMaxLength(64).IsFixedLength();
    b.Property(x => x.RevokedReason).HasColumnName("revoked_reason");
    b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

    b.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("ux_user_refresh_tokens_token_hash");
    b.HasIndex(x => x.UserId).HasDatabaseName("ix_user_refresh_tokens_user_id");
  }
}
