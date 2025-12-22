using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public sealed class AlertEventConfiguration : IEntityTypeConfiguration<AlertEvent>
{
  public void Configure(EntityTypeBuilder<AlertEvent> b)
  {
    b.ToTable("alert_events");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
    b.Property(x => x.UserId).HasColumnName("user_id");
    b.Property(x => x.AlertRuleId).HasColumnName("alert_rule_id");
    b.Property(x => x.Status).HasColumnName("status").HasConversion<short>();
    b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
    b.Property(x => x.Title).HasColumnName("title");
    b.Property(x => x.Body).HasColumnName("body");
    b.Property(x => x.PayloadJson).HasColumnName("payload").HasColumnType("jsonb");
    b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

    b.HasIndex(x => new { x.UserId, x.OccurredAt }).HasDatabaseName("ix_alert_events_user_id_occurred_at_desc");
    b.HasIndex(x => new { x.UserId, x.Status, x.OccurredAt }).HasDatabaseName("ix_alert_events_user_id_status_occurred_at_desc");
    b.HasIndex(x => new { x.AlertRuleId, x.OccurredAt }).HasDatabaseName("ix_alert_events_alert_rule_id_occurred_at_desc");
  }
}

