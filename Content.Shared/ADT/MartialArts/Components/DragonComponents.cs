using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.MartialArts;

[RegisterComponent, NetworkedComponent]
public sealed partial class DragonPowerBuffComponent : Component
{
    [DataField]
    public DamageModifierSet ModifierSet = new()
    {
        Coefficients =
        {
            {"Blunt", 0.6f},
            {"Slash", 0.6f},
            {"Piercing", 0.6f},
            {"Heat", 0.6f},
        },
    };

    [DataField]
    public float DamageMultiplier = 1.2f;

    [DataField]
    public TimeSpan AttackDamageBuffDuration = TimeSpan.FromSeconds(5);
}

[RegisterComponent, NetworkedComponent]
public sealed partial class DragonKungFuTimerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastMoveTime = TimeSpan.Zero;

    [DataField]
    public float MinVelocitySquared = 0.25f;

    [DataField]
    public TimeSpan PauseDuration = TimeSpan.FromSeconds(1f);

    [DataField]
    public TimeSpan BuffLength = TimeSpan.FromSeconds(5f);
}
