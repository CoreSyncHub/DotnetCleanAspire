using System.Diagnostics;

namespace Domain.Abstractions;

/// <summary>
/// Represents a unique identifier based on ULID.
/// </summary>
[DebuggerDisplay("{ToString()}")]
public readonly record struct Id : ISpanParsable<Id>, IComparable, IComparable<Id>
{
   private readonly Ulid _value;

   private Id(Ulid value) => _value = value;

   /// <summary>
   /// Return an empty ID.
   /// </summary>
   public static Id Empty => new(Ulid.Empty);

   /// <summary>
   /// Generates a new unique identifier.
   /// </summary>
   public static Id New() => new(Ulid.NewUlid());

   /// <summary>
   /// Indicates whether this ID is empty.
   /// </summary>
   public bool IsEmpty => _value == Ulid.Empty;

   /// <summary>
   /// Returns the underlying ULID value.
   /// </summary>
   public Ulid ToUlid() => _value;

   /// <summary>
   /// Convert in a <see cref="Guid"/>. Loss of temporal sortability.
   /// </summary>
   public Guid ToGuid() => _value.ToGuid();

   /// <summary>
   /// Creates an <see cref="Id"/> from a <see cref="Ulid"/>.
   /// </summary>
   public static Id FromUlid(Ulid ulid) => new(ulid);

   /// <summary>
   /// Creates an <see cref="Id"/> from a <see cref="Guid"/>.
   /// </summary>
   public static Id FromGuid(Guid value) => new(new Ulid(value));

   /// <summary>
   /// Explicit conversion from <see cref="Id"/> to <see cref="Ulid"/> or <see cref="Guid"/>.
   /// </summary>
   public static explicit operator Ulid(Id id) => id._value;

   /// <summary>
   /// Explicit conversion from <see cref="Id"/> to <see cref="Guid"/>.
   /// </summary>
   public static explicit operator Guid(Id id) => id.ToGuid();

   /// <summary>
   /// Explicit conversion from <see cref="Ulid"/> or <see cref="Guid"/> to <see cref="Id"/>.
   /// </summary>
   public static explicit operator Id(Ulid ulid) => FromUlid(ulid);

   /// <summary>
   /// Explicit conversion from <see cref="Guid"/> to <see cref="Id"/>.
   /// </summary>
   public static explicit operator Id(Guid value) => FromGuid(value);

   /// <inheritdoc/>
   public override string ToString() => _value.ToString();

   /// <inheritdoc cref="Id.ToString()"/>
   public string ToString(string? format, IFormatProvider? provider) => _value.ToString(format, provider);

   /// <inheritdoc/>
   public bool TryFormat(Span<char> destination, out int written, ReadOnlySpan<char> format, IFormatProvider? provider)
       => _value.TryFormat(destination, out written, format, provider);

   /// <summary>
   /// Parse a GUID or ULID string representation into an <see cref="Id"/>.
   /// </summary>
   /// <exception cref="FormatException">If the string is not a valid ULID or GUID.</exception>
   public static Id Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);

   /// <summary>
   /// Try to parse a GUID or ULID string representation into an <see cref="Id"/>.
   /// </summary>
   public static bool TryParse(string? s, IFormatProvider? provider, out Id result)
       => TryParse(s.AsSpan(), provider, out result);

   /// <summary>
   /// Optimized Parse method for ULID or GUID from a <see cref="ReadOnlySpan{Char}"/>
   /// </summary>
   public static Id Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
   {
      if (Ulid.TryParse(s, out Ulid ulid))
         return new Id(ulid);
      if (Guid.TryParse(s, out Guid guid))
         return FromGuid(guid);
      throw new FormatException($"Not a ULID or GUID: {s}");
   }

   /// <summary>
   /// Optimized TryParse method for ULID or GUID from a <see cref="ReadOnlySpan{Char}"/>
   /// </summary>
   public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Id result)
   {
      if (Ulid.TryParse(s, out Ulid ulid))
      { result = new Id(ulid); return true; }

      if (Guid.TryParse(s, out Guid guid))
      { result = FromGuid(guid); return true; }

      result = default;
      return false;
   }

   /// <inheritdoc/>
   public int CompareTo(object? obj)
   {
      return obj switch
      {
         null => 1,
         Id other => CompareTo(other),
         _ => throw new ArgumentException($"Object must be of type {nameof(Id)}", nameof(obj))
      };
   }

   /// <inheritdoc/>
   public int CompareTo(Id other) => _value.CompareTo(other._value);

   public static bool operator <(Id left, Id right)
   {
      return left.CompareTo(right) < 0;
   }

   public static bool operator <=(Id left, Id right)
   {
      return left.CompareTo(right) <= 0;
   }

   public static bool operator >(Id left, Id right)
   {
      return left.CompareTo(right) > 0;
   }

   public static bool operator >=(Id left, Id right)
   {
      return left.CompareTo(right) >= 0;
   }
}
