using Finance.Domain.Common;
using Finance.Domain.Enums;

namespace Finance.Domain.Entities;

public sealed class ImportRow : BaseEntity<long>, IUserOwnedEntity
{
  public Guid ImportId { get; set; }
  public Guid UserId { get; set; }
  public int RowIndex { get; set; }
  public int? PageNumber { get; set; }
  public string? RowSha256 { get; set; }
  public ImportRowStatus Status { get; set; }
  public string? RawText { get; set; }
  public string? RawDataJson { get; set; }
  public string? ErrorCode { get; set; }
  public string? ErrorMessage { get; set; }
}
