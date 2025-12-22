namespace Finance.Infrastructure.Jobs;

public sealed class HangfireOptions
{
  public const string SectionName = "Hangfire";
  public string SchemaName { get; set; } = "hangfire";
}

