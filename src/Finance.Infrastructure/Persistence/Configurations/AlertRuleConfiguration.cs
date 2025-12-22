using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public sealed class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
  public void Configure(EntityTypeBuilder<AlertRule> b)
  {
    b.ToTable("alert_rules");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
    b.Property(x => x.UserId).HasColumnName("user_id");
    b.Property(x => x.Type).HasColumnName("type").HasConversion<short>();
    b.Property(x => x.Name).HasColumnName("name");
    b.Property(x => x.IsActive).HasColumnName("is_active");
    b.Property(x => x.BudgetId).HasColumnName("budget_id");
    b.Property(x => x.CategoryId).HasColumnName("category_id");
    b.Property(x => x.AccountId).HasColumnName("account_id");
    b.Property(x => x.ThresholdAmount).HasColumnName("threshold_amount").HasPrecision(18, 2);
    b.Property(x => x.ThresholdPercent).HasColumnName("threshold_percent").HasPrecision(18, 2);
    b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

    b.HasIndex(x => new { x.UserId, x.IsActive }).HasDatabaseName("ix_alert_rules_user_id_active");
    b.HasIndex(x => x.BudgetId).HasDatabaseName("ix_alert_rules_budget_id");
    b.HasIndex(x => x.CategoryId).HasDatabaseName("ix_alert_rules_category_id");
    b.HasIndex(x => x.AccountId).HasDatabaseName("ix_alert_rules_account_id");
  }
}

