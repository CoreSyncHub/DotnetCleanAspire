namespace Application.Abstractions.Pagination;

/// <summary>
/// Represents a cursor-based pagination response.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
public sealed record CursorPageResponse<T>
{
   /// <summary>
   /// The items in the current page.
   /// </summary>
   public required IReadOnlyList<T> Items { get; init; }

   /// <summary>
   /// The cursor for the next page. Null if there is no next page.
   /// </summary>
   public string? NextCursor { get; init; }

   /// <summary>
   /// The cursor for the previous page. Null if there is no previous page.
   /// </summary>
   public string? PreviousCursor { get; init; }

   /// <summary>
   /// Indicates whether there are more items after the current page.
   /// </summary>
   public bool HasNextPage => NextCursor is not null;

   /// <summary>
   /// Indicates whether there are items before the current page.
   /// </summary>
   public bool HasPreviousPage => PreviousCursor is not null;

   /// <summary>
   /// The number of items in the current page.
   /// </summary>
   public int Count => Items.Count;

   /// <summary>
   /// Creates an empty page response.
   /// </summary>
   public static CursorPageResponse<T> Empty() => new()
   {
      Items = []
   };
}
