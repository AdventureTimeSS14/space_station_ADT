using Robust.Shared.GameStates;

namespace Content.Shared.ADT.BloodCough;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class BloodCoughComponent : Component
{
    public TimeSpan NextSecond = TimeSpan.Zero;

    [DataField("coughTimeMin"), ViewVariables(VVAccess.ReadWrite)]
    public int CoughTimeMin = 2;

    [DataField("coughTimeMax"), ViewVariables(VVAccess.ReadWrite)]
    public int CoughTimeMax = 12;

    [DataField("postingSayDamage")]
    public string? PostingSayDamage = "Кашляет кровью";

    public bool CheckCoughBlood = false;
}
