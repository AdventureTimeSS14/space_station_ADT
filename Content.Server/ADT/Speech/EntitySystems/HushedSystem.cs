using Content.Shared.ADT.Speech.EntitySystems;
using Content.Shared.Speech.Hushing;
using Content.Shared.StatusEffect;

namespace Content.Server.ADT.Speech.EntitySystems;

public sealed class HushedSystem : SharedHushedSystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    public override void DoHushed(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        _statusEffectsSystem.TryAddStatusEffect<HushedComponent>(uid, HushedKey, time, refresh, status);
    }
}
