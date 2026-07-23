using Robust.Shared.GameStates;

namespace Content.Shared._VG.Flammability;

[RegisterComponent, NetworkedComponent]
public sealed partial class FireImmunityComponent : Component
{
    public override bool SessionSpecific => true;
}