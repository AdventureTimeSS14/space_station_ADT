//

using Content.Shared.Heretic.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Lock;

public sealed partial class AccessDeniedSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public bool HasAccessDenied(EntityUid uid)
    {
        return _status.HasEffectComp<AccessDeniedStatusEffectComponent>(uid);
    }
}
