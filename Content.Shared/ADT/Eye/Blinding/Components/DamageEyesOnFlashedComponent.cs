namespace Content.Shared.ADT.Eye.Blinding;

[RegisterComponent]
public sealed partial class DamageEyesOnFlashedComponent : Component
{
    [DataField]
    public int FlashDamage = 1;

    public TimeSpan NextDamage = TimeSpan.Zero;
}
