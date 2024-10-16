using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.ADT.Ghostbar;

/// <summary>
/// прототип со списком карт для пулла гостбаров
/// </summary>
[Prototype("ghostBarMapPool")]
public sealed partial class GhostBarMapPoolPrototype : IPrototype
{

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("maps", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<GhostBarMapPrototype>), required: true)]
    public HashSet<string> Maps = new(0);
}
