using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Converters;

internal sealed class IdConverter : ValueConverter<Id, string>
{
   public IdConverter() : base(
      id => id.ToString(),
      value => Id.Parse(value, null)
   )
   { }
}
