using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.LegionCore;

[RegisterComponent]
public sealed partial class ADTRegenerativeCoreComponent : Component
{
    [DataField]
    public EntProtoId StatusEffect = "ADTStatusEffectRegenerativeCore";

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(60);

    [DataField]
    public DamageSpecifier InstantHeal = new();

    [DataField]
    public float ApplyDelay = 1f;

    [DataField]
    public float WeakenedMultiplier = 0.5f;

    [DataField]
    public bool RemoveStuns = true;

    [DataField]
    public bool CanImplant = true;

    [DataField]
    public float ImplantDelay = 5f;

    [DataField]
    public float ImplantCellularMultiplier = 1f;
}
