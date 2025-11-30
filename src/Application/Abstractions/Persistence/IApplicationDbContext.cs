using Domain.Todos.Entities;

namespace Application.Abstractions.Persistence;

/// <summary>
/// Abstraction for the application database context.
/// Implemented in the Infrastructure layer.
/// </summary>
public interface IApplicationDbContext
{
   /// <summary>
   /// Gets the Todos DbSet.
   /// </summary>
   DbSet<Todo> Todos { get; }

   /// <summary>
   /// Saves changes to the database.
   /// This method should also dispatch domain events after successful save.
   /// </summary>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>The number of state entries written to the database.</returns>
   Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
