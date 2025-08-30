using System.Threading;

namespace Content.Server.ADT.BloodCough;

[RegisterComponent]
public sealed partial class BloodCoughComponent : Component
{
    [DataField("coughTimeMin"), ViewVariables(VVAccess.ReadWrite)]
    public int CoughTimeMin = 5;

    [DataField("coughTimeMax"), ViewVariables(VVAccess.ReadWrite)]
    public int CoughTimeMax = 16;

    [DataField("postingSayDamage")]
    public string? PostingSayDamage = default;

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
