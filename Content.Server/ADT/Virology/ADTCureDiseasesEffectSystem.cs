using System.Linq;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Effects;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Virology;

public sealed class ADTCureDiseasesEffectSystem : EntityEffectSystem<ADTDiseaseCarrierComponent, ADTCureDiseases>
{
    [Dependency] private ADTDiseaseSystem _disease = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    protected override void Effect(Entity<ADTDiseaseCarrierComponent> entity, ref EntityEffectEvent<ADTCureDiseases> args)
    {
        foreach (var disease in entity.Comp.Diseases.ToList())
        {
            if (args.Effect.VirusesOnly)
            {
                if (!_proto.TryIndex(disease.Prototype, out var proto) || !proto.IsVirus)
                    continue;
            }

            _disease.Cure(entity.AsNullable(), disease, args.Effect.GiveImmunity);
        }
    }
}
