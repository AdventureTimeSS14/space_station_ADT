using Content.Shared.Damage;

namespace Content.Shared.ADT.Lavaland.LegionCore;

[RegisterComponent]
public sealed partial class ADTRegenerativeCoreStatusEffectComponent : Component
{
    [DataField]
    public DamageSpecifier Healing = new();

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables]
    public TimeSpan NextTick;
}
