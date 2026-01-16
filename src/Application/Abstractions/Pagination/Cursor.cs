using System.Text;
using System.Text.Json;

namespace Application.Abstractions.Pagination;

/// <summary>
/// Provides methods for encoding and decoding pagination cursors.
/// </summary>
public static class Cursor
{
   private static readonly JsonSerializerOptions JsonOptions = new()
   {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
   };

   /// <summary>
   /// Encodes cursor values into a Base64 string.
   /// </summary>
   /// <param name="values">The values to encode.</param>
   /// <returns>A Base64 encoded cursor string.</returns>
   public static string Encode(params object[] values)
   {
      string json = JsonSerializer.Serialize(values, JsonOptions);
      byte[] bytes = Encoding.UTF8.GetBytes(json);
      return Convert.ToBase64String(bytes);
   }

   /// <summary>
   /// Encodes a single Id into a cursor.
   /// </summary>
   /// <param name="id">The Id to encode.</param>
   /// <returns>A Base64 encoded cursor string.</returns>
   public static string Encode(Id id) => Encode(id.ToString());

   /// <summary>
   /// Encodes an Id with an additional sort value into a cursor.
   /// </summary>
   /// <typeparam name="TSortValue">The type of the sort value.</typeparam>
   /// <param name="id">The Id to encode.</param>
   /// <param name="sortValue">The additional sort value.</param>
   /// <returns>A Base64 encoded cursor string.</returns>
   public static string Encode<TSortValue>(Id id, TSortValue sortValue) => Encode(id.ToString(), sortValue!);

   /// <summary>
   /// Decodes a cursor string back into its component values.
   /// </summary>
   /// <param name="cursor">The Base64 encoded cursor string.</param>
   /// <returns>The decoded values as a JsonElement array.</returns>
   public static JsonElement[]? Decode(string? cursor)
   {
      if (string.IsNullOrWhiteSpace(cursor))
         return null;

      try
      {
         byte[] bytes = Convert.FromBase64String(cursor);
         string json = Encoding.UTF8.GetString(bytes);
         return JsonSerializer.Deserialize<JsonElement[]>(json, JsonOptions);
      }
      catch
      {
         return null;
      }
   }

   /// <summary>
   /// Decodes a cursor and extracts the Id.
   /// </summary>
   /// <param name="cursor">The cursor string.</param>
   /// <returns>The decoded Id, or null if invalid.</returns>
   public static Id? DecodeId(string? cursor)
   {
      JsonElement[]? values = Decode(cursor);
      if (values is null || values.Length is 0)
         return null;

      string? idString = values[0].GetString();
      return Id.TryParse(idString, null, out Id id) ? id : null;
   }

   /// <summary>
   /// Decodes a cursor and extracts the Id and sort value.
   /// </summary>
   /// <typeparam name="TSortValue">The type of the sort value.</typeparam>
   /// <param name="cursor">The cursor string.</param>
   /// <returns>A tuple containing the Id and sort value, or null if invalid.</returns>
   public static (Id Id, TSortValue SortValue)? DecodeWithSortValue<TSortValue>(string? cursor)
   {
      JsonElement[]? values = Decode(cursor);
      if (values is null || values.Length < 2)
         return null;

      string? idString = values[0].GetString();
      if (!Id.TryParse(idString, null, out Id id))
         return null;

      try
      {
         TSortValue? sortValue = values[1].Deserialize<TSortValue>(JsonOptions);
         return sortValue is null ? null : (id, sortValue);
      }
      catch
      {
         return null;
      }
   }
}
