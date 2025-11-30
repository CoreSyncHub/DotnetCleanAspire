namespace Application.Features.Todos.Dtos;

/// <summary>
/// Response for creating a Todo.
/// </summary>
/// <param name="Id">The ID of the created todo.</param>
/// <param name="Title">The title of the created todo.</param>
public sealed record CreateTodoDto(string Id, string Title);
