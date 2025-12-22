using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Finance.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> b)
  {
    b.ToTable("users");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
    b.Property(x => x.Email).HasColumnName("email").HasColumnType("citext");
    b.Property(x => x.DisplayName).HasColumnName("display_name");
    b.Property(x => x.PasswordHash).HasColumnName("password_hash");
    b.Property(x => x.Timezone).HasColumnName("timezone").HasDefaultValue("America/Sao_Paulo");
    b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength().HasDefaultValue("BRL");
    b.Property(x => x.DisplayPreferences)
      .HasColumnName("display_preferences")
      .HasColumnType("jsonb")
      .HasConversion(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<UserDisplayPreferences>(v, (JsonSerializerOptions?)null) ?? new UserDisplayPreferences());
    b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

    b.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ux_users_email");
  }
}
