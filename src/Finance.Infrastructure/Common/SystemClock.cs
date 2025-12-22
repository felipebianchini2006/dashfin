using Finance.Application.Abstractions;

namespace Finance.Infrastructure.Common;

public sealed class SystemClock : IClock
{
  public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

