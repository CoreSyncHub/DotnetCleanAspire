using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Extensions;

internal static class ModelConfigurationBuilderExtensions
{
   extension(ModelConfigurationBuilder configurationBuilder)
   {
      public void ConfigureIdConventions()
      {
         configurationBuilder.Properties<Id>()
             .HaveConversion<IdConverter>()
             .HaveColumnType("char(26)")
             .AreUnicode(false)
             .HaveMaxLength(26)
             .UseCollation("C");

         configurationBuilder.Properties<Id?>()
             .HaveConversion<IdConverter>()
             .HaveColumnType("char(26)")
             .AreUnicode(false)
             .HaveMaxLength(26)
             .UseCollation("C");
      }
   }
}
