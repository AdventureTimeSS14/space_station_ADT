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
    public int CoughTimeMax = 17;

    [DataField("postingSayDamage")]
    public string? PostingSayDamage = "Кашляет кровью";

    //[DataField("randomIntervalSpeak"), ViewVariables(VVAccess.ReadWrite)]
    public bool CheckCoughBlood = false;
}
