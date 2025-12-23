using System.Security.Cryptography;
using System.Text;

namespace Finance.Application.Imports.Processing;

public sealed record TransactionFingerprintResult(
  string Payload,
  string Hash,
  string DescriptionNormalized,
  string? SourceLineSha256,
  string? LegacyHash);

public sealed record TransactionFingerprintOptions(
  bool IncludeSourceLineHash = false);

public sealed class TransactionFingerprintBuilder
{
  private const string Version = "v2";

  public TransactionFingerprintResult Build(
    Guid userId,
    Guid accountId,
    DateTimeOffset occurredAt,
    decimal amount,
    string description,
    string? sourceLine = null,
    TransactionFingerprintOptions? options = null)
  {
    var descriptionNormalized = DescriptionNormalizer.Normalize(description);
    return BuildFromNormalized(userId, accountId, occurredAt, amount, descriptionNormalized, sourceLine, options);
  }

  public TransactionFingerprintResult BuildFromNormalized(
    Guid userId,
    Guid accountId,
    DateTimeOffset occurredAt,
    decimal amount,
    string descriptionNormalized,
    string? sourceLine = null,
    TransactionFingerprintOptions? options = null)
  {
    return BuildInternal(userId, accountId, occurredAt, amount, descriptionNormalized, null, null, sourceLine, options);
  }

  public TransactionFingerprintResult BuildWithLegacy(
    Guid userId,
    Guid accountId,
    DateTimeOffset occurredAt,
    decimal amount,
    string description,
    string currencyForLegacy,
    string? sourceLine = null,
    TransactionFingerprintOptions? options = null)
  {
    var descriptionNormalized = DescriptionNormalizer.Normalize(description);
    return BuildInternal(userId, accountId, occurredAt, amount, descriptionNormalized, description, currencyForLegacy, sourceLine, options);
  }

  private static TransactionFingerprintResult BuildInternal(
    Guid userId,
    Guid accountId,
    DateTimeOffset occurredAt,
    decimal amount,
    string descriptionNormalized,
    string? rawDescriptionForLegacy,
    string? currencyForLegacy,
    string? sourceLine,
    TransactionFingerprintOptions? options)
  {
    options ??= new TransactionFingerprintOptions();
    descriptionNormalized ??= string.Empty;

    var dateUtc = occurredAt.ToUniversalTime().Date;
    var cents = decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);

    var normalizedSourceLine = options.IncludeSourceLineHash ? NormalizeSourceLineForHash(sourceLine) : string.Empty;
    var sourceLineSha = normalizedSourceLine.Length == 0 ? null : HashText(normalizedSourceLine);
    var payload = sourceLineSha is null
      ? $"{Version}|{userId:D}|{accountId:D}|{dateUtc:yyyy-MM-dd}|{cents:0}|{descriptionNormalized}"
      : $"{Version}|{userId:D}|{accountId:D}|{dateUtc:yyyy-MM-dd}|{cents:0}|{descriptionNormalized}|SRC:{sourceLineSha}";

    var hash = HashText(payload);

    string? legacyHash = null;
    if (!string.IsNullOrWhiteSpace(currencyForLegacy) && rawDescriptionForLegacy is not null)
    {
      var legacyNormalized = LegacyDescriptionNormalizer.Normalize(rawDescriptionForLegacy);
      legacyHash = TransactionFingerprint.Create(userId, accountId, occurredAt, amount, currencyForLegacy, legacyNormalized);
    }

    return new TransactionFingerprintResult(payload, hash, descriptionNormalized, sourceLineSha, legacyHash);
  }

  private static string NormalizeSourceLineForHash(string? sourceLine)
  {
    if (string.IsNullOrWhiteSpace(sourceLine))
      return string.Empty;

    var sb = new StringBuilder(sourceLine.Length);
    var prevSpace = false;
    foreach (var ch in sourceLine.Trim())
    {
      if (char.IsWhiteSpace(ch))
      {
        if (!prevSpace)
        {
          sb.Append(' ');
          prevSpace = true;
        }
        continue;
      }

      prevSpace = false;
      sb.Append(ch);
    }

    return sb.ToString();
  }

  private static string HashText(string text)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }
}
