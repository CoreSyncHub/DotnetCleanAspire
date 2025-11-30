using System.Linq.Expressions;

namespace Application.Abstractions.Pagination;

/// <summary>
/// Extension methods for cursor-based pagination on IQueryable.
/// </summary>
public static class QueryableCursorExtensions
{
   /// <summary>
   /// Applies cursor-based pagination to a query, using Id as the cursor key.
   /// </summary>
   /// <typeparam name="T">The entity type.</typeparam>
   /// <param name="query">The queryable source.</param>
   /// <param name="request">The pagination request.</param>
   /// <param name="idSelector">A function to extract the Id from an entity.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>A cursor page response.</returns>
   public static async Task<CursorPageResponse<T>> ToCursorPageAsync<T>(
       this IQueryable<T> query,
       CursorPageRequest request,
       Expression<Func<T, Id>> idSelector,
       CancellationToken cancellationToken = default)
       where T : class
   {
      Id? cursorId = Cursor.DecodeId(request.Cursor);
      Func<T, Id> compiledIdSelector = idSelector.Compile();

      IQueryable<T> filteredQuery;

      if (cursorId.HasValue)
      {
         Id cursorValue = cursorId.Value;

         if (request.Direction == CursorDirection.Forward)
         {
            // Get items after cursor (Id > cursorId)
            filteredQuery = query
                .Where(BuildIdComparisonExpression(idSelector, cursorValue, isGreaterThan: true))
                .OrderBy(idSelector);
         }
         else
         {
            // Get items before cursor (Id < cursorId)
            filteredQuery = query
                .Where(BuildIdComparisonExpression(idSelector, cursorValue, isGreaterThan: false))
                .OrderByDescending(idSelector);
         }
      }
      else
      {
         // No cursor, start from beginning
         filteredQuery = query.OrderBy(idSelector);
      }

      // Fetch one extra item to check if there are more
      List<T> items = await filteredQuery
          .Take(request.PageSize + 1)
          .ToListAsync(cancellationToken);

      bool hasMore = items.Count > request.PageSize;

      if (hasMore)
      {
         items.RemoveAt(items.Count - 1);
      }

      // Reverse if navigating backward
      if (request.Direction == CursorDirection.Backward)
      {
         items.Reverse();
      }

      // Build cursors
      string? nextCursor = null;
      string? previousCursor = null;

      if (items.Count > 0)
      {
         Id firstId = compiledIdSelector(items[0]);
         Id lastId = compiledIdSelector(items[^1]);

         if (request.Direction == CursorDirection.Forward)
         {
            if (hasMore)
               nextCursor = Cursor.Encode(lastId);

            if (cursorId.HasValue)
               previousCursor = Cursor.Encode(firstId);
         }
         else
         {
            if (cursorId.HasValue)
               nextCursor = Cursor.Encode(lastId);

            if (hasMore)
               previousCursor = Cursor.Encode(firstId);
         }
      }

      return new CursorPageResponse<T>
      {
         Items = items,
         NextCursor = nextCursor,
         PreviousCursor = previousCursor
      };
   }

   /// <summary>
   /// Applies cursor-based pagination with a custom sort key.
   /// </summary>
   /// <typeparam name="T">The entity type.</typeparam>
   /// <typeparam name="TSortKey">The type of the sort key.</typeparam>
   /// <param name="query">The queryable source.</param>
   /// <param name="request">The pagination request.</param>
   /// <param name="idSelector">A function to extract the Id from an entity.</param>
   /// <param name="sortKeySelector">A function to extract the sort key from an entity.</param>
   /// <param name="descending">Whether to sort in descending order.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>A cursor page response.</returns>
   public static async Task<CursorPageResponse<T>> ToCursorPageAsync<T, TSortKey>(
       this IQueryable<T> query,
       CursorPageRequest request,
       Expression<Func<T, Id>> idSelector,
       Expression<Func<T, TSortKey>> sortKeySelector,
       bool descending = false,
       CancellationToken cancellationToken = default)
       where T : class
       where TSortKey : IComparable<TSortKey>
   {
      (Id Id, TSortKey SortValue)? cursorData = Cursor.DecodeWithSortValue<TSortKey>(request.Cursor);
      Func<T, Id> compiledIdSelector = idSelector.Compile();
      Func<T, TSortKey> compiledSortKeySelector = sortKeySelector.Compile();

      IQueryable<T> filteredQuery;

      if (cursorData.HasValue)
      {
         (Id cursorId, TSortKey? cursorSortValue) = cursorData.Value;

         if (request.Direction == CursorDirection.Forward)
         {
            if (descending)
            {
               // Descending: (sortKey < cursorSortKey) OR (sortKey == cursorSortKey AND Id > cursorId)
               filteredQuery = query
                   .Where(BuildCompositeComparisonExpression(
                       idSelector, sortKeySelector, cursorId, cursorSortValue, isDescending: true, isForward: true))
                   .OrderByDescending(sortKeySelector)
                   .ThenBy(idSelector);
            }
            else
            {
               // Ascending: (sortKey > cursorSortKey) OR (sortKey == cursorSortKey AND Id > cursorId)
               filteredQuery = query
                   .Where(BuildCompositeComparisonExpression(
                       idSelector, sortKeySelector, cursorId, cursorSortValue, isDescending: false, isForward: true))
                   .OrderBy(sortKeySelector)
                   .ThenBy(idSelector);
            }
         }
         else
         {
            if (descending)
            {
               // Backward descending
               filteredQuery = query
                   .Where(BuildCompositeComparisonExpression(
                       idSelector, sortKeySelector, cursorId, cursorSortValue, isDescending: true, isForward: false))
                   .OrderBy(sortKeySelector)
                   .ThenByDescending(idSelector);
            }
            else
            {
               // Backward ascending
               filteredQuery = query
                   .Where(BuildCompositeComparisonExpression(
                       idSelector, sortKeySelector, cursorId, cursorSortValue, isDescending: false, isForward: false))
                   .OrderByDescending(sortKeySelector)
                   .ThenByDescending(idSelector);
            }
         }
      }
      else
      {
         // No cursor, start from beginning
         filteredQuery = descending
             ? query.OrderByDescending(sortKeySelector).ThenBy(idSelector)
             : query.OrderBy(sortKeySelector).ThenBy(idSelector);
      }

      // Fetch one extra item to check if there are more
      List<T> items = await filteredQuery
          .Take(request.PageSize + 1)
          .ToListAsync(cancellationToken);

      bool hasMore = items.Count > request.PageSize;

      if (hasMore)
      {
         items.RemoveAt(items.Count - 1);
      }

      // Reverse if navigating backward
      if (request.Direction == CursorDirection.Backward)
      {
         items.Reverse();
      }

      // Build cursors
      string? nextCursor = null;
      string? previousCursor = null;

      if (items.Count > 0)
      {
         T first = items[0];
         T last = items[^1];

         string firstCursor = Cursor.Encode(compiledIdSelector(first), compiledSortKeySelector(first));
         string lastCursor = Cursor.Encode(compiledIdSelector(last), compiledSortKeySelector(last));

         if (request.Direction == CursorDirection.Forward)
         {
            if (hasMore)
               nextCursor = lastCursor;

            if (cursorData.HasValue)
               previousCursor = firstCursor;
         }
         else
         {
            if (cursorData.HasValue)
               nextCursor = lastCursor;

            if (hasMore)
               previousCursor = firstCursor;
         }
      }

      return new CursorPageResponse<T>
      {
         Items = items,
         NextCursor = nextCursor,
         PreviousCursor = previousCursor
      };
   }

   private static Expression<Func<T, bool>> BuildIdComparisonExpression<T>(
       Expression<Func<T, Id>> idSelector,
       Id cursorValue,
       bool isGreaterThan)
   {
      ParameterExpression parameter = idSelector.Parameters[0];
      Expression idAccess = idSelector.Body;
      ConstantExpression cursorConstant = Expression.Constant(cursorValue);

      BinaryExpression comparison = isGreaterThan
          ? Expression.GreaterThan(idAccess, cursorConstant)
          : Expression.LessThan(idAccess, cursorConstant);

      return Expression.Lambda<Func<T, bool>>(comparison, parameter);
   }

   private static Expression<Func<T, bool>> BuildCompositeComparisonExpression<T, TSortKey>(
       Expression<Func<T, Id>> idSelector,
       Expression<Func<T, TSortKey>> sortKeySelector,
       Id cursorId,
       TSortKey cursorSortValue,
       bool isDescending,
       bool isForward)
       where TSortKey : IComparable<TSortKey>
   {
      ParameterExpression parameter = idSelector.Parameters[0];

      // Rebind the expressions to use the same parameter
      Expression idAccess = RebindParameter(idSelector.Body, idSelector.Parameters[0], parameter);
      Expression sortKeyAccess = RebindParameter(sortKeySelector.Body, sortKeySelector.Parameters[0], parameter);

      ConstantExpression cursorIdConstant = Expression.Constant(cursorId);
      ConstantExpression cursorSortConstant = Expression.Constant(cursorSortValue, typeof(TSortKey));

      Expression sortKeyComparison;
      Expression idComparison;

      if (isForward)
      {
         if (isDescending)
         {
            // (sortKey < cursorSort) OR (sortKey == cursorSort AND id > cursorId)
            sortKeyComparison = Expression.LessThan(sortKeyAccess, cursorSortConstant);
            idComparison = Expression.GreaterThan(idAccess, cursorIdConstant);
         }
         else
         {
            // (sortKey > cursorSort) OR (sortKey == cursorSort AND id > cursorId)
            sortKeyComparison = Expression.GreaterThan(sortKeyAccess, cursorSortConstant);
            idComparison = Expression.GreaterThan(idAccess, cursorIdConstant);
         }
      }
      else
      {
         if (isDescending)
         {
            // (sortKey > cursorSort) OR (sortKey == cursorSort AND id < cursorId)
            sortKeyComparison = Expression.GreaterThan(sortKeyAccess, cursorSortConstant);
            idComparison = Expression.LessThan(idAccess, cursorIdConstant);
         }
         else
         {
            // (sortKey < cursorSort) OR (sortKey == cursorSort AND id < cursorId)
            sortKeyComparison = Expression.LessThan(sortKeyAccess, cursorSortConstant);
            idComparison = Expression.LessThan(idAccess, cursorIdConstant);
         }
      }

      BinaryExpression sortKeyEqual = Expression.Equal(sortKeyAccess, cursorSortConstant);
      BinaryExpression equalAndId = Expression.AndAlso(sortKeyEqual, idComparison);
      BinaryExpression combined = Expression.OrElse(sortKeyComparison, equalAndId);

      return Expression.Lambda<Func<T, bool>>(combined, parameter);
   }

   private static Expression RebindParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
   {
      return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
   }

   private sealed class ParameterReplacer : ExpressionVisitor
   {
      private readonly ParameterExpression _oldParameter;
      private readonly ParameterExpression _newParameter;

      public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
      {
         _oldParameter = oldParameter;
         _newParameter = newParameter;
      }

      protected override Expression VisitParameter(ParameterExpression node)
      {
         return node == _oldParameter ? _newParameter : base.VisitParameter(node);
      }
   }
}
