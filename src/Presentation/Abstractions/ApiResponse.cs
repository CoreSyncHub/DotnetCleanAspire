namespace Presentation.Abstractions;

/// <summary>
/// Standard API response envelope for successful responses.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
internal sealed record ApiResponse<T>
{
   /// <summary>
   /// The data payload of the response.
   /// </summary>
   public required T Data { get; init; }

   /// <summary>
   /// Optional metadata about the response (pagination, etc.).
   /// </summary>
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public object? Meta { get; init; }

   /// <summary>
   /// Creates a successful API response with the specified data.
   /// </summary>
   public static ApiResponse<T> From(T data, object? meta = null) => new() { Data = data, Meta = meta };
}

/// <summary>
/// Standard API response envelope for responses without data.
/// </summary>
internal sealed record ApiResponse
{
   /// <summary>
   /// Optional message for the response.
   /// </summary>
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public string? Message { get; init; }

   /// <summary>
   /// Creates a successful API response with an optional message.
   /// </summary>
   public static ApiResponse From(string? message = null) => new() { Message = message };
}
