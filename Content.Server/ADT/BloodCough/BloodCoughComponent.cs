using Robust.Shared.GameStates;

namespace Content.Server.ADT.BloodCough;

[RegisterComponent]
public sealed partial class BloodCoughComponent : Component
{
    public TimeSpan NextSecond = TimeSpan.Zero;

    [DataField("coughTimeMin"), ViewVariables(VVAccess.ReadWrite)]
    public int CoughTimeMin = 11;

    [DataField("coughTimeMax"), ViewVariables(VVAccess.ReadWrite)]
    public int CoughTimeMax = 17;

    [DataField("postingSayDamage")]
    public string? PostingSayDamage = default;

    public bool CheckCoughBlood = false;
}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
