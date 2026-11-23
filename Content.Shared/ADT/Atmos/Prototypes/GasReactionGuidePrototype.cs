using Content.Shared.Atmos.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Atmos.Prototypes;

[Prototype]
public sealed partial class GasReactionGuidePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public Dictionary<ProtoId<GasPrototype>, float> Consumes = new();

    [DataField]
    public Dictionary<ProtoId<GasPrototype>, float> Produces = new();

    [DataField]
    public GasReactionHeat Heat = GasReactionHeat.None;

    [DataField]
    public bool NobliumInhibited;

    [DataField]
    public LocId? Note;
}

public enum GasReactionHeat : byte
{
    None,
    Released,
    Absorbed,
}
