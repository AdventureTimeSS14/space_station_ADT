using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Weapons.Ranged.Upgrades.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTGunUpgradeResonatorComponent : Component
{
    [DataField]
    public EntProtoId FieldProto = "ADTResonanceFieldPKA";

    [DataField]
    public float BurstMultiplier = 0.25f;
}

[RegisterComponent]
public sealed partial class ADTProjectileResonatorComponent : Component
{
    [DataField]
    public EntProtoId FieldProto = "ADTResonanceFieldPKA";

    [DataField]
    public float BurstMultiplier = 0.25f;

    [ViewVariables]
    public ADTVolleyTrigger? Volley;
}
