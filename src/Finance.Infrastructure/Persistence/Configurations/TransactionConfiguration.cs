using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
  public void Configure(EntityTypeBuilder<Transaction> b)
  {
    b.ToTable("transactions");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
    b.Property(x => x.UserId).HasColumnName("user_id");
    b.Property(x => x.AccountId).HasColumnName("account_id");
    b.Property(x => x.CategoryId).HasColumnName("category_id");
    b.Property(x => x.ImportId).HasColumnName("import_id");
    b.Property(x => x.ImportRowId).HasColumnName("import_row_id");
    b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
    b.Property(x => x.Description).HasColumnName("description");
    b.Property(x => x.Notes).HasColumnName("notes");
    b.Property(x => x.IgnoreInDashboard).HasColumnName("ignore_in_dashboard").HasDefaultValue(false);
    b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
    b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength();
    b.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64).IsFixedLength();
    b.Property(x => x.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb");
    b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

    b.HasIndex(x => new { x.UserId, x.Fingerprint }).IsUnique().HasDatabaseName("ux_transactions_user_id_fingerprint");
    b.HasIndex(x => new { x.UserId, x.OccurredAt }).HasDatabaseName("ix_transactions_user_id_occurred_at_desc");
    b.HasIndex(x => new { x.UserId, x.AccountId, x.OccurredAt }).HasDatabaseName("ix_transactions_user_id_account_id_occurred_at_desc");
    b.HasIndex(x => new { x.UserId, x.CategoryId, x.OccurredAt }).HasDatabaseName("ix_transactions_user_id_category_id_occurred_at_desc");

    b.HasIndex(x => x.ImportId).HasDatabaseName("ix_transactions_import_id");
    b.HasIndex(x => x.ImportRowId).HasDatabaseName("ix_transactions_import_row_id");
  }
}
