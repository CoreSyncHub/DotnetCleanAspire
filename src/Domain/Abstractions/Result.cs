namespace Domain.Abstractions;

public enum ErrorType
{
   Failure,
   Validation,
   NotFound,
   Conflict,
   Unauthorized,
   Forbidden
}

public enum SuccessType
{
   Ok,
   Created,
   NoContent
}

#pragma warning disable CA1815 // Override equals and gethashcode are not necessary for this struct

public readonly struct Unit
{
   public static readonly Unit Value;
}

#pragma warning restore CA1815

public record ResultError(string Code, string Message, ErrorType Type = ErrorType.Failure);

public readonly record struct Result
{
   public static Result<Unit> Success(SuccessType type = SuccessType.Ok) => Result<Unit>.Success(Unit.Value, type);
   public static Result<T> Success<T>(T value, SuccessType type = SuccessType.Ok) => Result<T>.Success(value, type);
   public static Result<Unit> Failure(ResultError error) => Result<Unit>.Failure(error);
   public static Result<T> Failure<T>(ResultError error) => Result<T>.Failure(error);
}

public readonly record struct Result<T>
{
   public bool IsSuccess { get; }
   public bool IsFailure => !IsSuccess;
   public T? Value { get; }
   public ResultError? Error { get; }
   public SuccessType SuccessType { get; }

   private Result(T value, SuccessType successType = SuccessType.Ok)
   {
      IsSuccess = true;
      Value = value;
      SuccessType = successType;
   }

   private Result(ResultError error)
   {
      IsSuccess = false;
      Error = error;
      SuccessType = default;
   }

   public static Result<T> Success(T value, SuccessType type = SuccessType.Ok) => new(value, type);
   public static Result<T> Failure(ResultError error) => new(error);

   public static implicit operator Result<T>(T value) => Success(value);
   public static implicit operator Result<T>(ResultError error) => Failure(error);

   public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ResultError, TResult> onFailure)
       => IsSuccess ? onSuccess(Value!) : onFailure(Error!);

   public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
       => IsSuccess ? Result<TNew>.Success(mapper(Value!), SuccessType) : Result<TNew>.Failure(Error!);

   public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
       => IsSuccess ? binder(Value!) : Result<TNew>.Failure(Error!);
}
