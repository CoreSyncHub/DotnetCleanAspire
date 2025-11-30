using Domain.Todos.Errors;

namespace Domain.Todos.ValueObjects;

public sealed class TodoTitle : ValueObject
{
   /// <summary>
   /// Value of the Title.
   /// </summary>
   public string Value { get; }

   private TodoTitle(string value) => Value = value;

   public static Result<TodoTitle> Create(string value)
   {
      if (string.IsNullOrWhiteSpace(value))
      {
         return Result<TodoTitle>.Failure(TodoErrors.TitleRequired);
      }

      if (value.Length > 100)
      {
         return Result<TodoTitle>.Failure(TodoErrors.TitleTooLong);
      }

      return Result<TodoTitle>.Success(new TodoTitle(value));
   }

   public override string ToString() => Value;

   protected override IEnumerable<object> GetEqualityComponents()
   {
      yield return Value;
   }
}
