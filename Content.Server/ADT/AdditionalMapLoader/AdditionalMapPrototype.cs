using Content.Server.Maps;
using Robust.Shared.Prototypes;

// Author: by TornadoTech
namespace Content.Server.ADT.AdditionalMapLoader;

[Prototype("additionalMap")]
public sealed class AdditionalMapPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    [DataField("maps")]
    public List<ProtoId<GameMapPrototype>> MapProtoIds = new();
}
