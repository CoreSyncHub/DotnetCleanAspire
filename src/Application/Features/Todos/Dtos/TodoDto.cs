namespace Application.Features.Todos.Dtos;

/// <summary>
/// Common response DTO for Todo queries.
/// </summary>
/// <param name="Id">The todo ID.</param>
/// <param name="Title">The todo title.</param>
/// <param name="Status">The todo status.</param>
/// <param name="CreatedAt">When the todo was created.</param>
public sealed record TodoDto(
    string Id,
    string Title,
    string Status,
    DateTimeOffset CreatedAt);
