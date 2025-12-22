namespace Finance.Application.Imports.Processing;

public static class ImportLayoutDetector
{
  public static ImportLayout Detect(IEnumerable<string> allLines)
  {
    var text = string.Join('\n', allLines).ToLowerInvariant();

    // Conta
    if (text.Contains("nuconta") || text.Contains("conta do nubank") || text.Contains("extrato") && text.Contains("saldo"))
      return ImportLayout.NubankConta;

    // Cartão
    if (text.Contains("cartão nubank") || text.Contains("cartao nubank") || text.Contains("fatura") || text.Contains("pagamento mínimo") || text.Contains("vencimento"))
      return ImportLayout.NubankCartao;

    return ImportLayout.Unknown;
  }
}

