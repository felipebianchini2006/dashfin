using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> b)
  {
    b.ToTable("users");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
    b.Property(x => x.Email).HasColumnName("email");
    b.Property(x => x.DisplayName).HasColumnName("display_name");
    b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
  }
}

