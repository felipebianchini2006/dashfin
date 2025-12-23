using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
  public void Configure(EntityTypeBuilder<Category> b)
  {
    b.ToTable("categories");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
    b.Property(x => x.UserId).HasColumnName("user_id");
    b.Property(x => x.Name).HasColumnName("name").HasColumnType("citext");
    b.Property(x => x.ParentId).HasColumnName("parent_id");
    b.Property(x => x.Color).HasColumnName("color");
    b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

    b.HasIndex(x => new { x.UserId, x.Name }).IsUnique().HasDatabaseName("ux_categories_user_id_name");
    b.HasIndex(x => x.UserId).HasDatabaseName("ix_categories_user_id");

    b.HasOne<Category>()
      .WithMany()
      .HasForeignKey(x => x.ParentId)
      .OnDelete(DeleteBehavior.SetNull);
  }
}
