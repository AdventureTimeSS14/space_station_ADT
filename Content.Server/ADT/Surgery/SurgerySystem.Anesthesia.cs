using Content.Shared.Bed.Sleep;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgerySystem
{
    private static readonly EntProtoId AnesthetizedStatusEffect = "StatusEffectAnesthetized";
    private static readonly EntProtoId WeaklyAnesthetizedStatusEffect = "StatusEffectWeaklyAnesthetized";
    private const float NoAnestheticChanceMultiplier = 0.7f;
    private static readonly ProtoId<EmotePrototype> ScreamEmote = "Scream";

    private static readonly DamageSpecifier NoAnestheticPainDamage = new()
    {
        DamageDict = new()
        {
            { "Blunt", 4 },
            { "Slash", 3 },
            { "Piercing", 3 },
        },
    };

    private bool IsAnesthetized(EntityUid patient)
    {
        if (_status.HasStatusEffect(patient, AnesthetizedStatusEffect))
            return true;

        return _status.HasEffectComp<ForcedSleepingStatusEffectComponent>(patient);
    }

    private bool IsWeaklyAnesthetized(EntityUid patient)
    {
        return _status.HasStatusEffect(patient, WeaklyAnesthetizedStatusEffect);
    }
}
