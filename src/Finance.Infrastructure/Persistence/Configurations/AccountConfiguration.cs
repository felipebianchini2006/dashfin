using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
  public void Configure(EntityTypeBuilder<Account> b)
  {
    b.ToTable("accounts");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
    b.Property(x => x.UserId).HasColumnName("user_id");
    b.Property(x => x.Type).HasColumnName("type").HasConversion<short>();
    b.Property(x => x.Name).HasColumnName("name");
    b.Property(x => x.Institution).HasColumnName("institution");
    b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength();
    b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

    b.HasIndex(x => x.UserId).HasDatabaseName("ix_accounts_user_id");
  }
}

