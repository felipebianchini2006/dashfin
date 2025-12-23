using System.Globalization;
using System.Text.Json;
using Finance.Application.Abstractions;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Alerts.Generate;

public sealed class GenerateAlertsService
{
  private readonly IAppDbContext _db;
  private readonly IClock _clock;

  public GenerateAlertsService(IAppDbContext db, IClock clock)
  {
    _db = db;
    _clock = clock;
  }

  public async Task GenerateAsync(Guid userId, int year, int month, CancellationToken ct)
  {
    var monthDate = NormalizeMonth(new DateOnly(year, month, 1));
    var monthStart = new DateTimeOffset(monthDate.Year, monthDate.Month, 1, 0, 0, 0, TimeSpan.Zero);
    var monthEnd = monthStart.AddMonths(1);

    var rules = await _db.AlertRules
      .AsNoTracking()
      .Where(r => r.UserId == userId && r.IsActive)
      .ToListAsync(ct);

    if (rules.Count == 0)
      return;

    var categoryIds = rules
      .Select(r => r.CategoryId)
      .Where(x => x is not null)
      .Select(x => x!.Value)
      .Distinct()
      .ToList();

    var categories = await _db.Categories
      .AsNoTracking()
      .Where(c => c.UserId == userId && categoryIds.Contains(c.Id))
      .Select(c => new { c.Id, c.Name })
      .ToListAsync(ct);

    var categoryName = categories.ToDictionary(x => x.Id, x => x.Name);

    var spendQuery = _db.Transactions
      .AsNoTracking()
      .Where(t =>
        t.UserId == userId &&
        t.CategoryId != null &&
        categoryIds.Contains(t.CategoryId.Value) &&
        t.OccurredAt >= monthStart &&
        t.OccurredAt < monthEnd &&
        t.Amount < 0m &&
        !t.IgnoreInDashboard);

    var spendByCategory = await spendQuery
      .GroupBy(t => t.CategoryId!.Value)
      .Select(g => new { CategoryId = g.Key, Spent = -g.Sum(t => t.Amount) })
      .ToDictionaryAsync(x => x.CategoryId, x => x.Spent, ct);

    var spendByCategoryAccount = await spendQuery
      .GroupBy(t => new { CategoryId = t.CategoryId!.Value, t.AccountId })
      .Select(g => new { g.Key.CategoryId, g.Key.AccountId, Spent = -g.Sum(t => t.Amount) })
      .ToListAsync(ct);

    var spendByCatAcc = spendByCategoryAccount.ToDictionary(x => (x.CategoryId, x.AccountId), x => x.Spent);

    var budgets = await _db.Budgets
      .AsNoTracking()
      .Where(b => b.UserId == userId && b.Month == monthDate && categoryIds.Contains(b.CategoryId))
      .Select(b => new { b.CategoryId, b.LimitAmount })
      .ToListAsync(ct);

    var budgetByCategory = budgets.ToDictionary(x => x.CategoryId, x => x.LimitAmount);

    var candidates = new List<AlertEvent>();
    foreach (var rule in rules)
    {
      if (rule.CategoryId is null)
        continue;

      var catId = rule.CategoryId.Value;
      var spent = rule.AccountId is not null
        ? spendByCatAcc.GetValueOrDefault((catId, rule.AccountId.Value), 0m)
        : spendByCategory.GetValueOrDefault(catId, 0m);

      if (spent <= 0m)
        continue;

      if (rule.Type == AlertRuleType.OverBudget)
      {
        if (!budgetByCategory.TryGetValue(catId, out var limit) || limit <= 0m)
          continue;

        var warningAt = limit * 0.8m;
        if (spent >= warningAt)
          candidates.Add(CreateOverBudgetEvent(userId, rule.Id, monthDate, catId, spent, limit, isCritical: false));
        if (spent >= limit)
          candidates.Add(CreateOverBudgetEvent(userId, rule.Id, monthDate, catId, spent, limit, isCritical: true));
      }
      else if (rule.Type == AlertRuleType.Threshold)
      {
        var threshold = rule.ThresholdAmount;
        if (threshold is null || threshold.Value <= 0m)
          continue;

        var warningAt = threshold.Value * 0.8m;
        if (spent >= warningAt)
          candidates.Add(CreateThresholdEvent(userId, rule.Id, monthDate, catId, spent, threshold.Value, isCritical: false));
        if (spent >= threshold.Value)
          candidates.Add(CreateThresholdEvent(userId, rule.Id, monthDate, catId, spent, threshold.Value, isCritical: true));
      }
    }

    if (candidates.Count == 0)
      return;

    foreach (var e in candidates)
    {
      if (categoryName.TryGetValue(GetCategoryIdFromPayload(e.PayloadJson), out var name))
      {
        e.Title = e.Title.Replace("{category}", name, StringComparison.Ordinal);
        if (e.Body is not null)
          e.Body = e.Body.Replace("{category}", name, StringComparison.Ordinal);
      }
      else
      {
        e.Title = e.Title.Replace("{category}", "Categoria", StringComparison.Ordinal);
        if (e.Body is not null)
          e.Body = e.Body.Replace("{category}", "Categoria", StringComparison.Ordinal);
      }
    }

    var fingerprints = candidates.Select(x => x.Fingerprint).Distinct(StringComparer.Ordinal).ToList();
    var existing = await _db.AlertEvents
      .AsNoTracking()
      .Where(a => a.UserId == userId && fingerprints.Contains(a.Fingerprint))
      .Select(a => a.Fingerprint)
      .ToListAsync(ct);

    var existingSet = existing.ToHashSet(StringComparer.Ordinal);
    var toInsert = candidates.Where(c => !existingSet.Contains(c.Fingerprint)).ToList();
    if (toInsert.Count == 0)
      return;

    _db.AlertEvents.AddRange(toInsert);
    await _db.SaveChangesAsync(ct);
  }

  private AlertEvent CreateOverBudgetEvent(Guid userId, Guid ruleId, DateOnly month, Guid categoryId, decimal spent, decimal limit, bool isCritical)
  {
    var kind = isCritical ? AlertKinds.OverBudgetCritical : AlertKinds.OverBudgetWarning;
    var fingerprint = AlertFingerprint.Create(userId, ruleId, month, kind);
    var percent = limit == 0m ? 0m : spent / limit * 100m;
    var pctText = percent.ToString("0.#", CultureInfo.InvariantCulture);
    var spentText = spent.ToString("0.00", CultureInfo.InvariantCulture);
    var limitText = limit.ToString("0.00", CultureInfo.InvariantCulture);

    var title = isCritical
      ? "Orçamento estourado: {category} (" + month.ToString("yyyy-MM", CultureInfo.InvariantCulture) + ")"
      : "Orçamento perto do limite: {category} (" + month.ToString("yyyy-MM", CultureInfo.InvariantCulture) + ")";

    var body = $"Gasto atual: {spentText} de {limitText} ({pctText}%).";

    var payload = JsonSerializer.Serialize(new
    {
      kind,
      month = month.ToString("yyyy-MM-01", CultureInfo.InvariantCulture),
      categoryId,
      spentAmount = spent,
      limitAmount = limit,
      percent
    });

    return new AlertEvent
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      AlertRuleId = ruleId,
      Fingerprint = fingerprint,
      Status = AlertEventStatus.New,
      OccurredAt = _clock.UtcNow,
      Title = title,
      Body = body,
      PayloadJson = payload
    };
  }

  private AlertEvent CreateThresholdEvent(Guid userId, Guid ruleId, DateOnly month, Guid categoryId, decimal spent, decimal threshold, bool isCritical)
  {
    var kind = isCritical ? AlertKinds.ThresholdCritical : AlertKinds.ThresholdWarning;
    var fingerprint = AlertFingerprint.Create(userId, ruleId, month, kind);
    var percent = threshold == 0m ? 0m : spent / threshold * 100m;
    var pctText = percent.ToString("0.#", CultureInfo.InvariantCulture);
    var spentText = spent.ToString("0.00", CultureInfo.InvariantCulture);
    var thresholdText = threshold.ToString("0.00", CultureInfo.InvariantCulture);

    var title = isCritical
      ? "Limite excedido: {category} (" + month.ToString("yyyy-MM", CultureInfo.InvariantCulture) + ")"
      : "Limite perto do máximo: {category} (" + month.ToString("yyyy-MM", CultureInfo.InvariantCulture) + ")";

    var body = $"Gasto atual: {spentText} de {thresholdText} ({pctText}%).";

    var payload = JsonSerializer.Serialize(new
    {
      kind,
      month = month.ToString("yyyy-MM-01", CultureInfo.InvariantCulture),
      categoryId,
      spentAmount = spent,
      thresholdAmount = threshold,
      percent
    });

    return new AlertEvent
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      AlertRuleId = ruleId,
      Fingerprint = fingerprint,
      Status = AlertEventStatus.New,
      OccurredAt = _clock.UtcNow,
      Title = title,
      Body = body,
      PayloadJson = payload
    };
  }

  private static Guid GetCategoryIdFromPayload(string? payloadJson)
  {
    if (string.IsNullOrWhiteSpace(payloadJson))
      return Guid.Empty;

    try
    {
      using var doc = JsonDocument.Parse(payloadJson);
      if (doc.RootElement.TryGetProperty("categoryId", out var prop) && prop.ValueKind == JsonValueKind.String &&
          Guid.TryParse(prop.GetString(), out var id))
      {
        return id;
      }
    }
    catch
    {
      // ignore
    }

    return Guid.Empty;
  }

  private static DateOnly NormalizeMonth(DateOnly month) => new(month.Year, month.Month, 1);
}

