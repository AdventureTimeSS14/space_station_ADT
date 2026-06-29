using System.Threading;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.ADT.BloodCough;

[RegisterComponent]
[AutoGenerateComponentState]
[Access(typeof(BloodCoughSystem))]
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

    /// <summary>
    /// The time at which the next cough will occur
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextCough = TimeSpan.Zero;
}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
