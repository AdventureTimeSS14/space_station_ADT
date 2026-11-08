using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.BodyTypes;

[Prototype]
public sealed partial class BodyTypePrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<BodyTypePrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField]
    public LocId Name;

    [DataField]
    public int Order;

    [DataField]
    public List<ProtoId<SpeciesPrototype>> Species = new();

    [DataField]
    public ResPath Sprite;

    [DataField]
    public string State = string.Empty;

    [DataField]
    public Dictionary<Sex, string> SexStates = new();

    public string GetState(Sex sex)
    {
        return SexStates.GetValueOrDefault(sex, State);
    }
}
