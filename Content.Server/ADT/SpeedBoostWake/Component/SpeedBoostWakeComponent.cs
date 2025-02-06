namespace Content.Server.ADT.SpeedBoostWake;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class SpeedBoostWakeComponent : Component
{
    /// <summary>
    /// The entity's speed boost.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedModified = 3.0f;

    /// <summary>
    /// How many seconds the mob will be paralyzed for.
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWrite)]
    public float ParalyzeTime = 3.5f;
}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
