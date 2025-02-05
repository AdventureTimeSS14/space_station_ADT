namespace Content.Server.ADT.SlippingWake;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class SlippingWakeComponent : Component
{
    /// <summary>
    /// The entity's speed will be multiplied by this to slip it forwards.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LaunchForwardsMultiplier = 30.5f;
}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
