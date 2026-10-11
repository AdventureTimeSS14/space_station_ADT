namespace Content.Server.ADT.Implants.SpasezhnikovImplant;

[RegisterComponent]
public sealed partial class SpasezhnikovTrackerComponent : Component
{
    public bool WasTriggered;
    public float Duration;
    public float HpThreshold;
    public float MovementSpeedModifier;
    public float AttackSpeedModifier;
    public float CurrentDamage;
}
