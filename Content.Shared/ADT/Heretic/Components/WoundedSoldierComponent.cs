using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class WoundedSoldierComponent : Component
{
    [DataField]
    public float LifeStealMultiplier = 0.3f;

    [DataField]
    public float StaminaHealMultiplier = 0.5f;

    [DataField]
    public float OvertimeDamageThresholdRatio = 0.1f;

    [DataField]
    public DamageSpecifier DamageOverTime = new()
    {
        DamageDict =
        {
            { "Heat", 10 },
        },
    };

    [DataField]
    public LocId ExamineLoc = "wounded-soldier-effect-examine-message";
}
