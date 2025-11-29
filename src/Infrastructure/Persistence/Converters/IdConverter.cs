using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Converters;

#pragma warning disable CA1812
internal sealed class IdConverter : ValueConverter<Id, string>
{
   public IdConverter() : base(
      id => id.ToString(),
      value => Id.Parse(value, null)
   )
   { }
}
