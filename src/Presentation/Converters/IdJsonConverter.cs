namespace Presentation.Converters;

internal sealed class IdJsonConverter : JsonConverter<Id>
{
    public override Id Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => Id.Parse(reader.GetString() ?? throw new JsonException(), null);

    public override void Write(Utf8JsonWriter writer, Id value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
