namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTHitStunChanceComponent : Component
{
    [DataField]
    public float KnockdownChance;

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(3);

    [DataField]
    public float SiliconStunChance;

    [DataField]
    public TimeSpan SiliconStunTime = TimeSpan.FromSeconds(3);
}
