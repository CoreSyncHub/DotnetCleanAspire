namespace Domain.Abstractions;

public enum ErrorType
{
   Failure,
   Validation,
   NotFound,
   Conflict,
   Unauthorized
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
   public static Result<Unit> Success() => Result<Unit>.Success(Unit.Value);
   public static Result<T> Success<T>(T value) => Result<T>.Success(value);
   public static Result<Unit> Failure(ResultError error) => Result<Unit>.Failure(error);
   public static Result<T> Failure<T>(ResultError error) => Result<T>.Failure(error);
}

public readonly record struct Result<T>
{
   public bool IsSuccess { get; }
   public bool IsFailure => !IsSuccess;
   public T? Value { get; }
   public ResultError? Error { get; }

   private Result(T value)
   {
      IsSuccess = true;
      Value = value;
   }

   private Result(ResultError error)
   {
      IsSuccess = false;
      Error = error;
   }

   public static Result<T> Success(T value) => new(value);
   public static Result<T> Failure(ResultError error) => new(error);

   public static implicit operator Result<T>(T value) => Success(value);
   public static implicit operator Result<T>(ResultError error) => Failure(error);

   public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ResultError, TResult> onFailure)
           => IsSuccess ? onSuccess(Value!) : onFailure(Error!);

   public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
       => IsSuccess ? Result<TNew>.Success(mapper(Value!)) : Result<TNew>.Failure(Error!);

   public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
       => IsSuccess ? binder(Value!) : Result<TNew>.Failure(Error!);
}
