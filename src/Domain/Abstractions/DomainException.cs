namespace Domain.Abstractions;

/// <summary>
/// Represents errors that occur during domain operations.
/// Classics domain errors should use Result pattern instead of exceptions.
/// </summary>
public abstract class DomainException : Exception
{
   /// <summary>
   /// Initializes a new instance of the <see cref="DomainException"/> class.
   /// </summary>
   protected DomainException()
   {
   }

   /// <summary>
   /// Initializes a new instance of the <see cref="DomainException"/> class with a specified error message.
   /// </summary>
   /// <param name="message">The message that describes the error.</param>
   protected DomainException(string message) : base(message)
   {
   }

   /// <summary>
   /// Initializes a new instance of the <see cref="DomainException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
   /// </summary>
   /// <param name="message">The message that describes the error.</param>
   /// <param name="innerException">The exception that is the cause of the current exception.</param>
   protected DomainException(string message, Exception innerException) : base(message, innerException)
   {
   }
}

/// <summary>
/// Represents an exception that is thrown when a domain invariant is violated.
/// </summary>
public sealed class InvariantViolationException : DomainException
{
   /// <summary>
   /// Initializes a new instance of the <see cref="InvariantViolationException"/> class.
   /// </summary>
   public InvariantViolationException()
   {
   }

   /// <summary>
   /// Initializes a new instance of the <see cref="InvariantViolationException"/> class with a specified error message.
   /// </summary>
   /// <param name="message">The message that describes the error.</param>
   public InvariantViolationException(string message) : base(message)
   {
   }

   /// <summary>
   /// Initializes a new instance of the <see cref="InvariantViolationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
   /// </summary>
   /// <param name="message">The message that describes the error.</param>
   /// <param name="innerException">The exception that is the cause of the current exception.</param>
   public InvariantViolationException(string message, Exception innerException) : base(message, innerException)
   {
   }
}
