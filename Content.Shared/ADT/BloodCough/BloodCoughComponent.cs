using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.BloodCough;

[RegisterComponent]
[NetworkedComponent]
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
