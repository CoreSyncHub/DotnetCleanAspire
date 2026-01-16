namespace Domain.Abstractions;

/// <summary>
/// Marker interface for result types to enable type-safe caching without reflection.
/// </summary>
public interface IResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    bool IsSuccess { get; }
}
