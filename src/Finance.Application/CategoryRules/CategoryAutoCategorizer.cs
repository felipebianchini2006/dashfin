using System.Text.RegularExpressions;
using Finance.Application.Abstractions;
using Finance.Application.Imports.Processing;
using Finance.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.CategoryRules;

public sealed class CategoryAutoCategorizer
{
  private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(50);

  private readonly IAppDbContext _db;

  public CategoryAutoCategorizer(IAppDbContext db) => _db = db;

  public async Task<CompiledCategoryRules> LoadAsync(Guid userId, CancellationToken ct)
  {
    var rules = await _db.CategoryRules
      .AsNoTracking()
      .Where(r => r.UserId == userId && r.IsActive)
      .OrderBy(r => r.Priority)
      .ThenByDescending(r => r.CreatedAt)
      .Select(r => new
      {
        r.Id,
        r.CategoryId,
        r.MatchType,
        r.Pattern,
        r.AccountId,
        r.Priority,
        r.MinAmount,
        r.MaxAmount
      })
      .ToListAsync(ct);

    var compiled = new List<CompiledCategoryRules.CompiledRule>(rules.Count);
    foreach (var r in rules)
    {
      var pattern = r.Pattern;
      if (r.MatchType == CategoryRuleMatchType.Contains)
      {
        pattern = DescriptionNormalizer.Normalize(pattern);
        if (pattern.Length == 0)
          continue;
      }

      Regex? regex = null;
      if (r.MatchType == CategoryRuleMatchType.Regex)
      {
        try
        {
          regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, RegexTimeout);
        }
        catch (ArgumentException)
        {
          continue;
        }
      }

      compiled.Add(new CompiledCategoryRules.CompiledRule(
        r.Id,
        r.CategoryId,
        r.MatchType,
        pattern,
        regex,
        r.AccountId,
        r.Priority,
        r.MinAmount,
        r.MaxAmount));
    }

    return new CompiledCategoryRules(compiled);
  }
}

public sealed class CompiledCategoryRules
{
  internal sealed record CompiledRule(
    Guid RuleId,
    Guid CategoryId,
    CategoryRuleMatchType MatchType,
    string Pattern,
    Regex? Regex,
    Guid? AccountId,
    int Priority,
    decimal? MinAmount,
    decimal? MaxAmount);

  private readonly IReadOnlyList<CompiledRule> _rules;

  public CompiledCategoryRules(IReadOnlyList<CompiledRule> rules) => _rules = rules;

  public Guid? MatchCategoryId(Guid accountId, string descriptionNormalized, decimal amount)
  {
    if (string.IsNullOrWhiteSpace(descriptionNormalized))
      return null;

    for (var i = 0; i < _rules.Count; i++)
    {
      var rule = _rules[i];

      if (rule.AccountId is not null && rule.AccountId.Value != accountId)
        continue;

      if (rule.MinAmount is not null && Math.Abs(amount) < rule.MinAmount.Value)
        continue;
      if (rule.MaxAmount is not null && Math.Abs(amount) > rule.MaxAmount.Value)
        continue;

      if (rule.MatchType == CategoryRuleMatchType.Contains)
      {
        if (descriptionNormalized.Contains(rule.Pattern, StringComparison.Ordinal))
          return rule.CategoryId;
      }
      else if (rule.MatchType == CategoryRuleMatchType.Regex)
      {
        try
        {
          if (rule.Regex is not null && rule.Regex.IsMatch(descriptionNormalized))
            return rule.CategoryId;
        }
        catch (RegexMatchTimeoutException)
        {
          continue;
        }
      }
    }

    return null;
  }
}
