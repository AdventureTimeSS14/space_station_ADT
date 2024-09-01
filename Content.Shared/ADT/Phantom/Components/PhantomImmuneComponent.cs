using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Content.Shared.Antag;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Phantom.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PhantomImmuneComponent : Component
{
    [DataField("statusIcon")]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "PhantomImmune";

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;
}
