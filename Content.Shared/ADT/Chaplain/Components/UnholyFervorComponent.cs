using Content.Shared.ADT.Damage;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Chaplain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class UnholyFervorComponent : Component
{
    [DataField]
    public DamageModifierSet IncomingModifiers = new()
    {
        Coefficients = new()
        {
            { "Blunt", 0.85f },
            { "Slash", 0.85f },
            { "Piercing", 0.85f },
            { "Heat", 0.85f },
            { "Cold", 0.85f },
            { "Shock", 0.85f },
            { "Caustic", 0.85f },
            { "Poison", 0.85f },
            { "Holy", 0.85f },
        },
    };

    [DataField]
    public DamageModifierSet MeleeModifiers = new()
    {
        Coefficients = new()
        {
            { "Blunt", 1.15f },
            { "Slash", 1.15f },
            { "Piercing", 1.15f },
            { "Heat", 1.15f },
            { "Cold", 1.15f },
            { "Shock", 1.15f },
            { "Caustic", 1.15f },
            { "Poison", 1.15f },
        },
    };

    [DataField]
    public float ActionDelayMultiplier = 0.75f;

    [ViewVariables]
    public Dictionary<EntityUid, TimeSpan?> OriginalDelays = new();

    [DataField]
    public GroupHealSpecifier Regen = new()
    {
        GroupHealDict = new()
        {
            { "Brute", -1 },
            { "Burn", -1 },
        },
    };

    [DataField]
    public TimeSpan RegenDelay = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRegen;
}
