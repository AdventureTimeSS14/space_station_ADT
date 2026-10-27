using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class CauterizeEffect : SurgeryStepEffect
{
    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<BloodstreamComponent>(patient, out var bloodstream))
            return;

        if (bloodstream.BleedAmount <= 0)
            return;

        entityManager.System<BloodstreamSystem>().TryModifyBleedAmount((patient, bloodstream), -bloodstream.BleedAmount);
        entityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("bloodstream-component-wounds-cauterized"), patient, patient, PopupType.Medium);
        entityManager.System<SharedAudioSystem>().PlayPredicted(bloodstream.BloodHealedSound, patient, surgeon);
    }
}
