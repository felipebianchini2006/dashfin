using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
  public void Configure(EntityTypeBuilder<ImportBatch> b)
  {
    b.ToTable("imports");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
    b.Property(x => x.UserId).HasColumnName("user_id");
    b.Property(x => x.AccountId).HasColumnName("account_id");
    b.Property(x => x.Status).HasColumnName("status").HasConversion(
      v => v.ToString().ToUpperInvariant(),
      v => Enum.Parse<Finance.Domain.Enums.ImportStatus>(v, ignoreCase: true));
    b.Property(x => x.FileName).HasColumnName("file_name");
    b.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes");
    b.Property(x => x.FileSha256).HasColumnName("file_sha256").HasMaxLength(64).IsFixedLength();
    b.Property(x => x.StorageProvider).HasColumnName("storage_provider");
    b.Property(x => x.StorageKey).HasColumnName("storage_key");
    b.Property(x => x.SummaryJson).HasColumnName("summary_json").HasColumnType("jsonb");
    b.Property(x => x.ProcessedAt).HasColumnName("processed_at");
    b.Property(x => x.ErrorMessage).HasColumnName("error_message");
    b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

    b.HasIndex(x => new { x.UserId, x.CreatedAt }).HasDatabaseName("ix_imports_user_id_created_at_desc");
    b.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("ix_imports_user_id_status");
    b.HasIndex(x => new { x.AccountId, x.CreatedAt }).HasDatabaseName("ix_imports_account_id_created_at_desc");
    b.HasIndex(x => new { x.UserId, x.FileSha256 }).IsUnique().HasDatabaseName("ux_imports_user_id_file_sha256");
  }
}
