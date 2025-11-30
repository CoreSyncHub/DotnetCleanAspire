namespace Application.Abstractions.Pagination;

/// <summary>
/// Represents a cursor-based pagination request.
/// </summary>
public sealed record CursorPageRequest
{
   /// <summary>
   /// The cursor pointing to the current position.
   /// Null for the first page.
   /// </summary>
   public string? Cursor { get; init; }

   /// <summary>
   /// The number of items per page.
   /// </summary>
   public int PageSize { get; init; } = 20;

   /// <summary>
   /// The direction of navigation.
   /// </summary>
   public CursorDirection Direction { get; init; } = CursorDirection.Forward;

   /// <summary>
   /// Creates a request for the first page.
   /// </summary>
   public static CursorPageRequest First(int pageSize = 20) => new() { PageSize = pageSize };

   /// <summary>
   /// Creates a request for the next page.
   /// </summary>
   public static CursorPageRequest Next(string cursor, int pageSize = 20) => new()
   {
      Cursor = cursor,
      PageSize = pageSize,
      Direction = CursorDirection.Forward
   };

   /// <summary>
   /// Creates a request for the previous page.
   /// </summary>
   public static CursorPageRequest Previous(string cursor, int pageSize = 20) => new()
   {
      Cursor = cursor,
      PageSize = pageSize,
      Direction = CursorDirection.Backward
   };
}
