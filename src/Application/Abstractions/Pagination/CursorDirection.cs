namespace Application.Abstractions.Pagination;

/// <summary>
/// Specifies the direction of cursor-based pagination.
/// </summary>
public enum CursorDirection
{
   /// <summary>
   /// Navigate forward (next page).
   /// </summary>
   Forward,

   /// <summary>
   /// Navigate backward (previous page).
   /// </summary>
   Backward
}
