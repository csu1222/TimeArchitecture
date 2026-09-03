using System;

public interface ITimeProvider
{
    DateTime UtcNow { get; }
}
