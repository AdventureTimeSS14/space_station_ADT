namespace Content.Server.ADT.Eye.Blinding;

[RegisterComponent]
[Access(typeof(DamageEyesOnFlashSystem))]
public sealed partial class DamageEyesOnFlashedComponent : Component
{
    [DataField]
    public int FlashDamage = 1;

    public TimeSpan NextDamage = TimeSpan.Zero;
}
