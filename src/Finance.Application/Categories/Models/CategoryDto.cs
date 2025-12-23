namespace Finance.Application.Categories.Models;

public sealed record CategoryDto(
  Guid Id,
  string Name,
  Guid? ParentId,
  string? Color);

