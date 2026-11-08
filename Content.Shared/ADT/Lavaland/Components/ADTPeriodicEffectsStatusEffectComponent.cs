using Content.Shared.EntityEffects;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTPeriodicEffectsStatusEffectComponent : Component
{
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables]
    public TimeSpan NextTick;
}
