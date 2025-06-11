using Robust.Shared.GameStates;

namespace Content.Shared.ADT.MindShield;

[RegisterComponent, NetworkedComponent]
public sealed partial class MindShieldMalfunctioningComponent : Component
{
    public TimeSpan EndTime = TimeSpan.Zero;
}
