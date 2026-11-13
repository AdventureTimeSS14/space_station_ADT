using Content.Shared.ADT.Shadowling;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class RemoveShadowTumorEffect : SurgeryStepEffect
{
    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (entityManager.TryGetComponent<ADTLesserShadowlingComponent>(patient, out var lesser))
        {
            if (surgeon is { } user)
                Backlash(patient, lesser, user, entityManager);

            return;
        }

        var body = entityManager.System<BodySystem>();
        foreach (var organ in body.EnumerateOrgans<ADTShadowTumorComponent>(patient))
        {
            body.TryRemoveOrgan(organ.Owner);
            break;
        }
    }

    private static void Backlash(EntityUid patient, ADTLesserShadowlingComponent lesser, EntityUid surgeon, IEntityManager entityManager)
    {
        var popup = entityManager.System<SharedPopupSystem>();
        popup.PopupEntity(Loc.GetString("shadowling-dethrall-backlash-thrall"), patient, patient, PopupType.LargeCaution);
        popup.PopupEntity(Loc.GetString("shadowling-dethrall-backlash-surgeon", ("target", patient)), surgeon, surgeon, PopupType.LargeCaution);

        entityManager.System<SharedStunSystem>().TryKnockdown(surgeon, lesser.SurgeryBacklashStun, true);

        var backlash = new DamageSpecifier();
        backlash.DamageDict.Add("Blunt", lesser.SurgeryBacklashDamage);
        entityManager.System<DamageableSystem>().TryChangeDamage(surgeon, backlash, true, origin: patient);
    }
}
