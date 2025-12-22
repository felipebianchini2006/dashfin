namespace Finance.Application.Imports.Processing;

public sealed record ParsedTransaction(
  DateTimeOffset OccurredAt,
  string Description,
  decimal Amount,
  string Currency,
  string DescriptionNormalized,
  string Fingerprint,
  string SourceLine);

