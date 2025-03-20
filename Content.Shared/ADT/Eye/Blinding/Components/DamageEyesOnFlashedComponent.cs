using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Eye.Blinding;

[RegisterComponent, NetworkedComponent]
public sealed partial class DamageEyesOnFlashedComponent : Component
{
    [DataField]
    public int FlashDamage = 1;

    public TimeSpan NextDamage = TimeSpan.Zero;
}
