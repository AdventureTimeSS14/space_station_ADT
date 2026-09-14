using Content.Shared.ADT.Construction.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Construction.Components;

/// <summary>
/// Rapid Part Exchanger: lets the user swap machine parts in assembled machines (or finish a machine frame).
/// </summary>
[RegisterComponent]
public sealed partial class PartExchangerComponent : Component
{
    [DataField]
    public float ExchangeDuration = 3f;

    [DataField]
    public bool DoDistanceCheck = true;

    [DataField]
    public bool RequireOpenPanel = true;

    [DataField]
    public SoundSpecifier ExchangeSound = new SoundPathSpecifier("/Audio/Items/rped.ogg");

    [DataField]
    public EntProtoId? ExchangeBeamPrototype;

    [DataField]
    public List<ProtoId<MachinePartPrototype>> FilteredParts = new();
}
