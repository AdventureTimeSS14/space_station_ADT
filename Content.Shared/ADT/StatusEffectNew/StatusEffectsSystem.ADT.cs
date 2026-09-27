using Robust.Shared.Prototypes;

namespace Content.Shared.StatusEffectNew;

public sealed partial class StatusEffectsSystem
{
    public bool TrySetStatusEffectStartTime(EntityUid target, EntProtoId effectProto, TimeSpan startTime)
    {
        if (!TryGetStatusEffect(target, effectProto, out var effect))
            return false;

        SetStatusEffectStartTime(effect.Value, startTime);
        return true;
    }
}
