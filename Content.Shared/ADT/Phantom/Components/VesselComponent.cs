using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Content.Shared.Antag;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Phantom.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VesselComponent : Component
{
    [DataField]
    public EntityUid Phantom = new EntityUid();

    [DataField("vesselStatusIcon")]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "PhantomVesselFaction";

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;
}
