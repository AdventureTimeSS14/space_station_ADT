using Robust.Shared.GameStates;
using Content.Shared.DoAfter;

namespace Content.Shared.ADT.ReliveResuscitation;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReliveResuscitationComponent : Component
{
    /// <summary>
    /// How long it takes to apply the damage.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delay")]
    public float Delay = 3f;
}

