using Finance.Application.Transactions.Models;

namespace Finance.Application.Transactions.Update;

public sealed record UpdateTransactionResultDto(
  TransactionDto Transaction,
  CategoryRuleSuggestionDto? SuggestedRule);

