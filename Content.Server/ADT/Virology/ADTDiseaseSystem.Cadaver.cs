using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Virology;

public sealed partial class ADTDiseaseSystem
{
    public static readonly ProtoId<ADTDiseasePrototype> CadaverDisease = "ADTDiseaseCadaver";

    public TimeSpan RotCheckInterval = TimeSpan.FromSeconds(2);

    public float RotInfectionChance = 0.003f;

    private TimeSpan _nextRotCheck;

    private void UpdateRotting(TimeSpan now)
    {
        if (now < _nextRotCheck)
            return;

        _nextRotCheck = now + RotCheckInterval;

        if (!_proto.TryIndex(CadaverDisease, out var proto))
            return;

        var query = EntityQueryEnumerator<RottingComponent, PerishableComponent>();
        while (query.MoveNext(out var uid, out var rotting, out var perishable))
        {
            if (!HasComp<HumanoidProfileComponent>(uid) && !HasComp<ADTVirusSusceptibleComponent>(uid))
                continue;

            var level = _rotting.RotStage(uid, rotting, perishable);
            if (level <= 0)
                continue;

            if (!_random.Prob(Math.Clamp(RotInfectionChance * level, 0f, 1f)))
                continue;

            var targets = new HashSet<Entity<MobStateComponent>>();
            _lookup.GetEntitiesInRange(Transform(uid).Coordinates, level, targets);

            foreach (var target in targets)
            {
                if (target.Owner == uid)
                    continue;

                Contract(target, ADTDisease.FromPrototype(proto), ADTDiseaseSpread.Contact, protectionCheck: true);
            }
        }
    }
}
