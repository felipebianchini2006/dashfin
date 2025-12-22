using Finance.Application.Abstractions;

namespace Finance.Application.Tests;

internal sealed class TestClock : IClock
{
  public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}

