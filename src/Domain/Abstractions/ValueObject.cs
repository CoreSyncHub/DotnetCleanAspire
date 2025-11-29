namespace Domain.Abstractions;

public abstract class ValueObject : IEquatable<ValueObject>
{
   protected static bool EqualOperator(ValueObject? left, ValueObject? right)
   {
      if (left is null ^ right is null)
      {
         return false;
      }

      return left?.Equals(right!) != false;
   }

   public static bool operator ==(ValueObject? a, ValueObject? b) => EqualOperator(a!, b!);
   public static bool operator !=(ValueObject? a, ValueObject? b) => !EqualOperator(a!, b!);

   public bool Equals(ValueObject? other) =>
       other is not null && GetType() == other.GetType() && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

   public override bool Equals(object? obj) => Equals(obj as ValueObject);

   public override int GetHashCode()
   {
      var h = new HashCode();
      foreach (object c in GetEqualityComponents())
         h.Add(c);
      return h.ToHashCode();
   }

   protected abstract IEnumerable<object> GetEqualityComponents();
}
