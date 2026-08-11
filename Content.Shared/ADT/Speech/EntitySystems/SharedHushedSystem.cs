using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Speech.EntitySystems;

public abstract class SharedHushedSystem : EntitySystem
{
    public static readonly ProtoId<StatusEffectPrototype> HushedKey = "Hushed";

    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    /// <summary>
    /// Makes the entity whisper while the effect lasts. Implemented on the server: HushedComponent
    /// must live on the entity itself (old status effect system), because ChatSystem checks for it
    /// directly on the speaker and the new StatusEffectNew system does not relay it.
    /// </summary>
    public virtual void DoHushed(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
    }

    public void DoRemoveHushed(EntityUid uid)
    {
        _statusEffectsSystem.TryRemoveStatusEffect(uid, HushedKey);
    }
}
