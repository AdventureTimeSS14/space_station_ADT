using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Speech.EntitySystems;

public abstract class SharedWeaknessSystem : EntitySystem
{
    public static readonly ProtoId<StatusEffectPrototype> WeaknessKey = "Weakness";

    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    // For code in shared... I imagine we ain't getting accent prediction anytime soon so let's not bother.
    public virtual void DoWeakness(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
    }

    public virtual void DoRemoveWeaknessTime(EntityUid uid, double timeRemoved)
    {
        _statusEffectsSystem.TryRemoveTime(uid, WeaknessKey, TimeSpan.FromSeconds(timeRemoved));
    }

    public void DoRemoveWeakness(EntityUid uid, double timeRemoved)
    {
        _statusEffectsSystem.TryRemoveStatusEffect(uid, WeaknessKey);
    }
}
