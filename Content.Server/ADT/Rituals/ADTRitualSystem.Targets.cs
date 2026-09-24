using System.Linq;
using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.ADT.Rituals;
using Content.Shared.Humanoid;
using Robust.Shared.Random;

namespace Content.Server.ADT.Rituals;

public sealed partial class ADTRitualSystem
{
    public List<EntityUid> GetTargets(ADTRitualArgs args, ADTRitualTarget target)
    {
        switch (target)
        {
            case ADTRitualTarget.Invoker:
                return new List<EntityUid> { args.Invoker };

            case ADTRitualTarget.Invokers:
                return args.Invokers.ToList();

            case ADTRitualTarget.UsedThings:
                return args.UsedThings.ToList();

            case ADTRitualTarget.Tribe:
                return GetOnMap(args.Object, tribe: true);

            case ADTRitualTarget.RandomTribesman:
                return PickOne(GetOnMap(args.Object, tribe: true));

            case ADTRitualTarget.Outsiders:
                return GetOnMap(args.Object, tribe: false);

            case ADTRitualTarget.RandomOutsider:
                return PickOne(GetOnMap(args.Object, tribe: false));

            case ADTRitualTarget.InRange:
                return GetInRange(args);

            default:
                return new List<EntityUid>();
        }
    }

    private List<EntityUid> GetOnMap(EntityUid ritualObject, bool tribe)
    {
        var result = new List<EntityUid>();
        var map = Transform(ritualObject).MapID;

        var query = EntityQueryEnumerator<HumanoidProfileComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (Transform(uid).MapID != map)
                continue;

            if (_mobState.IsDead(uid))
                continue;

            if (HasComp<ADTAshWalkerComponent>(uid) != tribe)
                continue;

            result.Add(uid);
        }

        return result;
    }

    private List<EntityUid> GetInRange(ADTRitualArgs args)
    {
        var result = new List<EntityUid>();

        if (!_proto.TryIndex<ADTRitualPrototype>(args.Ritual.ID, out var ritual))
            return result;

        var nearby = new HashSet<Entity<HumanoidProfileComponent>>();
        _lookup.GetEntitiesInRange(Transform(args.Object).Coordinates, ritual.FindingRange, nearby);

        foreach (var found in nearby)
        {
            result.Add(found.Owner);
        }

        return result;
    }

    private List<EntityUid> PickOne(List<EntityUid> from)
    {
        return from.Count == 0
            ? from
            : new List<EntityUid> { _random.Pick(from) };
    }
}
