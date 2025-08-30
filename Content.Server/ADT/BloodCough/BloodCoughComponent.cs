using System.Threading;

namespace Content.Server.ADT.BloodCough;

[RegisterComponent, AutoGenerateComponentState(true)]
public sealed partial class BloodCoughComponent : Component
{
    public TimeSpan NextSecond = TimeSpan.Zero;

    [DataField("coughTimeMin"), ViewVariables(VVAccess.ReadWrite)]
    public int CoughTimeMin = 5;

    [DataField("coughTimeMax"), ViewVariables(VVAccess.ReadWrite)]
    public int CoughTimeMax = 16;

    [DataField("postingSayDamage")]
    public string? PostingSayDamage = default;

    [AutoNetworkedField]
    public bool CheckCoughBlood = false;

    /// <summary>
    /// Token source for managing the timer cancellation
    /// </summary>
    public CancellationTokenSource? TokenSource;
}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
