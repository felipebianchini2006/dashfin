using System.Text.RegularExpressions;
using Finance.Application.Abstractions;
using Finance.Application.CategoryRules.Models;
using Finance.Application.Common;
using Finance.Application.Imports.Processing;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.CategoryRules.Create;

internal sealed class CreateCategoryRuleCommandHandler : IRequestHandler<CreateCategoryRuleCommand, Result<CategoryRuleDto>>
{
  private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(50);

  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public CreateCategoryRuleCommandHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<CategoryRuleDto>> Handle(CreateCategoryRuleCommand request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<CategoryRuleDto>(Error.Unauthorized());

    var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.UserId == userId.Value, ct);
    if (!categoryExists)
      return Result.Fail<CategoryRuleDto>(Error.Validation("Category not found."));

    var pattern = request.Pattern.Trim();
    if (pattern.Length == 0)
      return Result.Fail<CategoryRuleDto>(Error.Validation("Pattern is required."));

    if (request.MatchType == CategoryRuleMatchType.Contains)
    {
      pattern = DescriptionNormalizer.Normalize(pattern);
      if (pattern.Length == 0)
        return Result.Fail<CategoryRuleDto>(Error.Validation("Pattern is too broad."));
    }
    else if (request.MatchType == CategoryRuleMatchType.Regex)
    {
      try
      {
        _ = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, RegexTimeout);
      }
      catch (ArgumentException ex)
      {
        return Result.Fail<CategoryRuleDto>(Error.Validation($"Invalid regex: {ex.Message}"));
      }
    }

    var rule = new CategoryRule
    {
      Id = Guid.NewGuid(),
      UserId = userId.Value,
      CategoryId = request.CategoryId,
      MatchType = request.MatchType,
      Pattern = pattern,
      Priority = request.Priority,
      IsActive = request.IsActive
    };

    _db.CategoryRules.Add(rule);
    await _db.SaveChangesAsync(ct);

    return Result.Ok(new CategoryRuleDto(
      rule.Id,
      rule.CategoryId,
      rule.MatchType,
      rule.Pattern,
      rule.Priority,
      rule.IsActive,
      rule.AccountId,
      rule.MinAmount,
      rule.MaxAmount,
      rule.CreatedAt));
  }
}

