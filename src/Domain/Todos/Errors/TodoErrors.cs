namespace Domain.Todos.Errors;

/// <summary>
/// Domain errors related to the Todo aggregate.
/// </summary>
public static class TodoErrors
{
    /// <summary>
    /// Error codes for the Todo aggregate.
    /// </summary>
    public static class Codes
    {
        public const string NotFound = "Todo.NotFound";
        public const string AlreadyCompleted = "Todo.AlreadyCompleted";
        public const string TitleRequired = "Todo.TitleRequired";
        public const string TitleTooLong = "Todo.TitleTooLong";
        public const string IdRequired = "Todo.IdRequired";
    }

    public static ResultError NotFound(Id id) => new(
        Codes.NotFound,
        "Todo with ID '{0}' was not found.",
        [id],
        ErrorType.NotFound);

    public static ResultError AlreadyCompleted => new(
        Codes.AlreadyCompleted,
        "Todo is already completed.",
        ErrorType.Validation);

    public static ResultError TitleRequired => new(
        Codes.TitleRequired,
        "Todo title is required.",
        ErrorType.Validation);

    public static ResultError TitleTooLong => new(
        Codes.TitleTooLong,
        "Todo title cannot exceed 100 characters.",
        ErrorType.Validation);

    public static ResultError IdRequired => new(
        Codes.IdRequired,
        "Todo ID is required.",
        ErrorType.Validation);
}
