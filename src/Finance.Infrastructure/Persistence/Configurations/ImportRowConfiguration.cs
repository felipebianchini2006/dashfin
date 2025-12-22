using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public sealed class ImportRowConfiguration : IEntityTypeConfiguration<ImportRow>
{
  public void Configure(EntityTypeBuilder<ImportRow> b)
  {
    b.ToTable("import_rows");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
    b.Property(x => x.ImportId).HasColumnName("import_id");
    b.Property(x => x.UserId).HasColumnName("user_id");
    b.Property(x => x.RowIndex).HasColumnName("row_index");
    b.Property(x => x.PageNumber).HasColumnName("page_number");
    b.Property(x => x.RowSha256).HasColumnName("row_sha256").HasMaxLength(64).IsFixedLength();
    b.Property(x => x.Status).HasColumnName("status").HasConversion(
      v => v.ToString().ToUpperInvariant(),
      v => Enum.Parse<Finance.Domain.Enums.ImportRowStatus>(v, ignoreCase: true));
    b.Property(x => x.RawText).HasColumnName("raw_text");
    b.Property(x => x.RawDataJson).HasColumnName("raw_data").HasColumnType("jsonb");
    b.Property(x => x.ErrorCode).HasColumnName("error_code");
    b.Property(x => x.ErrorMessage).HasColumnName("error_message");
    b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

    b.HasIndex(x => new { x.ImportId, x.RowIndex }).IsUnique().HasDatabaseName("ux_import_rows_import_id_row_index");
    b.HasIndex(x => new { x.UserId, x.CreatedAt }).HasDatabaseName("ix_import_rows_user_id_created_at_desc");
    b.HasIndex(x => new { x.ImportId, x.Status }).HasDatabaseName("ix_import_rows_import_id_status");
  }
}

