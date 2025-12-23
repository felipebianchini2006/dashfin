using Finance.Application.Imports.Processing;

namespace Finance.Application.Transactions.Update;

internal static class CategoryRuleSuggestionBuilder
{
  private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
  {
    "PIX",
    "PAGAMENTO",
    "PAG",
    "COMPRA",
    "TRANSFERENCIA",
    "TRANSFERENCIAS",
    "TRANSFERENCIASINTERNO",
    "INTERNO",
    "RECEBIDO",
    "RECEBIDA",
    "ENVIADO",
    "ENVIADA",
    "TED",
    "DOC",
    "DEBITO",
    "CREDITO",
    "ESTORNO",
    "REEMBOLSO",
    "REFUND",
    "SALDO"
  };

  public static string? BuildContainsPattern(string description)
  {
    var normalized = DescriptionNormalizer.Normalize(description);
    if (normalized.Length == 0)
      return null;

    var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (tokens.Length == 0)
      return null;

    var chosen = new List<string>(3);
    foreach (var token in tokens)
    {
      if (token.Length < 3)
        continue;
      if (StopWords.Contains(token))
        continue;
      chosen.Add(token);
      if (chosen.Count == 3)
        break;
    }

    if (chosen.Count == 0)
      return null;

    return string.Join(' ', chosen);
  }
}

