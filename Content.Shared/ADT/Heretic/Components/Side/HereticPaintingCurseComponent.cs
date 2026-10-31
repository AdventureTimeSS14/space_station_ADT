using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticPaintingCurseComponent : Component
{
    [DataField]
    public HereticPaintingKind Kind = HereticPaintingKind.Weeping;

    [DataField]
    public float TimeLeft = 120f;
}
