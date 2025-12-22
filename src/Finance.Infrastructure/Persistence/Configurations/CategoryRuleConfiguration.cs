using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public sealed class CategoryRuleConfiguration : IEntityTypeConfiguration<CategoryRule>
{
  public void Configure(EntityTypeBuilder<CategoryRule> b)
  {
    b.ToTable("category_rules");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
    b.Property(x => x.UserId).HasColumnName("user_id");
    b.Property(x => x.CategoryId).HasColumnName("category_id");
    b.Property(x => x.MatchType).HasColumnName("match_type").HasConversion<short>();
    b.Property(x => x.Pattern).HasColumnName("pattern");
    b.Property(x => x.AccountId).HasColumnName("account_id");
    b.Property(x => x.Priority).HasColumnName("priority");
    b.Property(x => x.IsActive).HasColumnName("is_active");
    b.Property(x => x.MinAmount).HasColumnName("min_amount").HasPrecision(18, 2);
    b.Property(x => x.MaxAmount).HasColumnName("max_amount").HasPrecision(18, 2);
    b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

    b.HasIndex(x => new { x.UserId, x.IsActive, x.Priority }).HasDatabaseName("ix_category_rules_user_id_active_priority");
    b.HasIndex(x => x.CategoryId).HasDatabaseName("ix_category_rules_category_id");
    b.HasIndex(x => x.AccountId).HasDatabaseName("ix_category_rules_account_id");
  }
}

