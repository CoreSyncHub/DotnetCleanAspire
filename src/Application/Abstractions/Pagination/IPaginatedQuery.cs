namespace Application.Abstractions.Pagination;

/// <summary>
/// Represents a query that returns paginated results using cursor-based pagination.
/// </summary>
/// <typeparam name="TItem">The type of items in the paginated response.</typeparam>
/// <remarks>
/// Implement this interface for queries that need pagination support.
/// The pagination parameters are automatically available through <see cref="Pagination"/>.
/// </remarks>
public interface IPaginatedQuery<TItem> : IQuery<CursorPageResponse<TItem>>
{
   /// <summary>
   /// Gets the pagination request parameters.
   /// </summary>
   /// <value>
   /// The cursor-based pagination request containing cursor position, page size, and direction.
   /// Defaults to <see cref="CursorPageRequest.First()"/> if not specified.
   /// </value>
   CursorPageRequest Pagination { get; }
}
