using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
  public void Configure(EntityTypeBuilder<Budget> b)
  {
    b.ToTable("budgets");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
    b.Property(x => x.UserId).HasColumnName("user_id");
    b.Property(x => x.CategoryId).HasColumnName("category_id");
    b.Property(x => x.Month).HasColumnName("month").HasColumnType("date");
    b.Property(x => x.LimitAmount).HasColumnName("limit_amount").HasPrecision(18, 2);
    b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

    b.HasCheckConstraint("ck_budgets_month_first_day", "month = date_trunc('month', month)::date");
    b.HasIndex(x => new { x.UserId, x.CategoryId, x.Month }).IsUnique().HasDatabaseName("ux_budgets_user_id_category_id_month");
    b.HasIndex(x => new { x.UserId, x.Month }).HasDatabaseName("ix_budgets_user_id_month");
  }
}

