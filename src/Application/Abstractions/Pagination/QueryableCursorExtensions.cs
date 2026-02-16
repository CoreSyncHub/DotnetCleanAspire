using System.Linq.Expressions;

namespace Application.Abstractions.Pagination;

/// <summary>
/// Extension methods for cursor-based pagination on IQueryable.
/// </summary>
internal static class QueryableCursorExtensions
{
   extension<T>(IQueryable<T> query) where T : class
   {
      /// <summary>
      /// Applies cursor-based pagination to a query, using Id as the cursor key.
      /// </summary>
      /// <param name="request">The pagination request.</param>
      /// <param name="idSelector">A function to extract the Id from an entity.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>A cursor page response.</returns>
      public async Task<CursorPageResponse<T>> ToCursorPageAsync(
          CursorPageRequest request,
          Expression<Func<T, Id>> idSelector,
          CancellationToken cancellationToken = default)
      {
         Id? cursorId = Cursor.DecodeId(request.Cursor);
         Func<T, Id> compiledIdSelector = idSelector.Compile();

         IQueryable<T> filteredQuery = ApplyIdBasedCursorFilter(query, request, idSelector, cursorId);

         List<T> items = await FetchItemsAsync(filteredQuery, request.PageSize, cancellationToken);
         bool hasMore = TrimExtraItem(items, request.PageSize);
         ReverseIfBackward(items, request.Direction);

         (string? nextCursor, string? previousCursor) = BuildIdCursors(
             items, compiledIdSelector, request.Direction, hasMore, cursorId.HasValue);

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
      /// <typeparam name="TSortKey">The type of the sort key.</typeparam>
      /// <param name="request">The pagination request.</param>
      /// <param name="idSelector">A function to extract the Id from an entity.</param>
      /// <param name="sortKeySelector">A function to extract the sort key from an entity.</param>
      /// <param name="descending">Whether to sort in descending order.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>A cursor page response.</returns>
      public async Task<CursorPageResponse<T>> ToCursorPageAsync<TSortKey>(
          CursorPageRequest request,
          Expression<Func<T, Id>> idSelector,
          Expression<Func<T, TSortKey>> sortKeySelector,
          bool descending = false,
          CancellationToken cancellationToken = default)
          where TSortKey : IComparable<TSortKey>
      {
         (Id Id, TSortKey SortValue)? cursorData = Cursor.DecodeWithSortValue<TSortKey>(request.Cursor);
         Func<T, Id> compiledIdSelector = idSelector.Compile();
         Func<T, TSortKey> compiledSortKeySelector = sortKeySelector.Compile();

         IQueryable<T> filteredQuery = ApplySortKeyBasedCursorFilter(
             query, request, idSelector, sortKeySelector, cursorData, descending);

         List<T> items = await FetchItemsAsync(filteredQuery, request.PageSize, cancellationToken);
         bool hasMore = TrimExtraItem(items, request.PageSize);
         ReverseIfBackward(items, request.Direction);

         (string? nextCursor, string? previousCursor) = BuildSortKeyCursors(
             items, compiledIdSelector, compiledSortKeySelector, request.Direction, hasMore, cursorData.HasValue);

         return new CursorPageResponse<T>
         {
            Items = items,
            NextCursor = nextCursor,
            PreviousCursor = previousCursor
         };
      }
   }

   private static IQueryable<T> ApplyIdBasedCursorFilter<T>(
       IQueryable<T> query,
       CursorPageRequest request,
       Expression<Func<T, Id>> idSelector,
       Id? cursorId) where T : class
   {
      if (!cursorId.HasValue)
      {
         return query.OrderBy(idSelector);
      }

      Id cursorValue = cursorId.Value;
      bool isForward = request.Direction is CursorDirection.Forward;

      return isForward
          ? query
              .Where(BuildIdComparisonExpression(idSelector, cursorValue, isGreaterThan: true))
              .OrderBy(idSelector)
          : query
              .Where(BuildIdComparisonExpression(idSelector, cursorValue, isGreaterThan: false))
              .OrderByDescending(idSelector);
   }

   private static IQueryable<T> ApplySortKeyBasedCursorFilter<T, TSortKey>(
       IQueryable<T> query,
       CursorPageRequest request,
       Expression<Func<T, Id>> idSelector,
       Expression<Func<T, TSortKey>> sortKeySelector,
       (Id Id, TSortKey SortValue)? cursorData,
       bool descending)
       where T : class
       where TSortKey : IComparable<TSortKey>
   {
      if (!cursorData.HasValue)
      {
         return ApplyInitialOrdering(query, idSelector, sortKeySelector, descending);
      }

      (Id cursorId, TSortKey? cursorSortValue) = cursorData.Value;
      bool isForward = request.Direction is CursorDirection.Forward;

      return ApplyCursorFilterWithOrdering(
          query, idSelector, sortKeySelector, cursorId, cursorSortValue, descending, isForward);
   }

   private static IOrderedQueryable<T> ApplyInitialOrdering<T, TSortKey>(
       IQueryable<T> query,
       Expression<Func<T, Id>> idSelector,
       Expression<Func<T, TSortKey>> sortKeySelector,
       bool descending)
       where T : class
       where TSortKey : IComparable<TSortKey>
   {
      return descending
          ? query.OrderByDescending(sortKeySelector).ThenBy(idSelector)
          : query.OrderBy(sortKeySelector).ThenBy(idSelector);
   }

   private static IQueryable<T> ApplyCursorFilterWithOrdering<T, TSortKey>(
       IQueryable<T> query,
       Expression<Func<T, Id>> idSelector,
       Expression<Func<T, TSortKey>> sortKeySelector,
       Id cursorId,
       TSortKey cursorSortValue,
       bool descending,
       bool isForward)
       where T : class
       where TSortKey : IComparable<TSortKey>
   {
      Expression<Func<T, bool>> filter = BuildCompositeComparisonExpression(
          idSelector, sortKeySelector, cursorId, cursorSortValue, descending, isForward);

      IQueryable<T> filtered = query.Where(filter);

      return (descending, isForward) switch
      {
         (true, true) => filtered.OrderByDescending(sortKeySelector).ThenBy(idSelector),
         (false, true) => filtered.OrderBy(sortKeySelector).ThenBy(idSelector),
         (true, false) => filtered.OrderBy(sortKeySelector).ThenByDescending(idSelector),
         (false, false) => filtered.OrderByDescending(sortKeySelector).ThenByDescending(idSelector)
      };
   }

   private static async Task<List<T>> FetchItemsAsync<T>(
       IQueryable<T> query,
       int pageSize,
       CancellationToken cancellationToken)
   {
      return await query
          .Take(pageSize + 1)
          .ToListAsync(cancellationToken);
   }

   private static bool TrimExtraItem<T>(List<T> items, int pageSize)
   {
      bool hasMore = items.Count > pageSize;
      if (hasMore)
      {
         items.RemoveAt(items.Count - 1);
      }

      return hasMore;
   }

   private static void ReverseIfBackward<T>(List<T> items, CursorDirection direction)
   {
      if (direction is CursorDirection.Backward)
      {
         items.Reverse();
      }
   }

   private static (string? NextCursor, string? PreviousCursor) BuildIdCursors<T>(
       List<T> items,
       Func<T, Id> idSelector,
       CursorDirection direction,
       bool hasMore,
       bool hasCursor)
   {
      if (items.Count == 0)
      {
         return (null, null);
      }

      Id firstId = idSelector(items[0]);
      Id lastId = idSelector(items[^1]);

      return direction is CursorDirection.Forward
          ? (hasMore ? Cursor.Encode(lastId) : null, hasCursor ? Cursor.Encode(firstId) : null)
          : (hasCursor ? Cursor.Encode(lastId) : null, hasMore ? Cursor.Encode(firstId) : null);
   }

   private static (string? NextCursor, string? PreviousCursor) BuildSortKeyCursors<T, TSortKey>(
       List<T> items,
       Func<T, Id> idSelector,
       Func<T, TSortKey> sortKeySelector,
       CursorDirection direction,
       bool hasMore,
       bool hasCursor)
   {
      if (items.Count == 0)
      {
         return (null, null);
      }

      T first = items[0];
      T last = items[^1];
      string firstCursor = Cursor.Encode(idSelector(first), sortKeySelector(first));
      string lastCursor = Cursor.Encode(idSelector(last), sortKeySelector(last));

      return direction is CursorDirection.Forward
          ? (hasMore ? lastCursor : null, hasCursor ? firstCursor : null)
          : (hasCursor ? lastCursor : null, hasMore ? firstCursor : null);
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

      Expression idAccess = RebindParameter(idSelector.Body, idSelector.Parameters[0], parameter);
      Expression sortKeyAccess = RebindParameter(sortKeySelector.Body, sortKeySelector.Parameters[0], parameter);

      ConstantExpression cursorIdConstant = Expression.Constant(cursorId);
      ConstantExpression cursorSortConstant = Expression.Constant(cursorSortValue, typeof(TSortKey));

      (Expression sortKeyComparison, Expression idComparison) = BuildComparisonExpressions(
          idAccess, sortKeyAccess, cursorIdConstant, cursorSortConstant, isDescending, isForward);

      BinaryExpression sortKeyEqual = Expression.Equal(sortKeyAccess, cursorSortConstant);
      BinaryExpression equalAndId = Expression.AndAlso(sortKeyEqual, idComparison);
      BinaryExpression combined = Expression.OrElse(sortKeyComparison, equalAndId);

      return Expression.Lambda<Func<T, bool>>(combined, parameter);
   }

   private static (Expression SortKeyComparison, Expression IdComparison) BuildComparisonExpressions(
       Expression idAccess,
       Expression sortKeyAccess,
       ConstantExpression cursorIdConstant,
       ConstantExpression cursorSortConstant,
       bool isDescending,
       bool isForward)
   {
      bool useLessThanForSortKey = isForward ? isDescending : !isDescending;
      bool useGreaterThanForId = isForward;

      Expression sortKeyComparison = useLessThanForSortKey
          ? Expression.LessThan(sortKeyAccess, cursorSortConstant)
          : Expression.GreaterThan(sortKeyAccess, cursorSortConstant);

      Expression idComparison = useGreaterThanForId
          ? Expression.GreaterThan(idAccess, cursorIdConstant)
          : Expression.LessThan(idAccess, cursorIdConstant);

      return (sortKeyComparison, idComparison);
   }

   private static Expression RebindParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
   {
      return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
   }

   private sealed class ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter) : ExpressionVisitor
   {

      protected override Expression VisitParameter(ParameterExpression node)
      {
         return node == oldParameter ? newParameter : base.VisitParameter(node);
      }
   }
}
