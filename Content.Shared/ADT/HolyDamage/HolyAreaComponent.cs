namespace Content.Shared.ADT.HolyDamage;

[RegisterComponent]
public sealed partial class HolyAreaComponent : Component
{
    [DataField]
    public float FireStacks = 2.5f;

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(10);
}
