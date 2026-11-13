using System.Text.Json;
using System.Text.Json.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.GuideGenerator;

public sealed class ProtoIdJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ProtoId<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(ProtoIdJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]);
        return (JsonConverter) Activator.CreateInstance(converterType)!;
    }
}

public sealed class ProtoIdJsonConverter<T> : JsonConverter<ProtoId<T>> where T : class, IPrototype
{
    public override ProtoId<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, ProtoId<T> value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id);
    }

    public override ProtoId<T> ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, ProtoId<T> value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.Id);
    }
}
