using Content.Server.Heretic.EntitySystems;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Components.Side;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Components;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
namespace Content.Server.ADT.Heretic.Ritual;
public sealed partial class RitualShatteredBehavior : RitualCustomBehavior
{
    private EntityUid _corpse;
    public override bool Execute(RitualData args, out string? outstr)
    {
        _corpse = EntityUid.Invalid;
        var entMan = args.EntityManager;
        var lookup = entMan.System<EntityLookupSystem>();
        var mindSys = entMan.System<SharedMindSystem>();
        foreach (var ent in lookup.GetEntitiesInRange(args.Platform, 1.5f))
        {
            if (ent == args.Performer)
                continue;
            if (!entMan.HasComponent<HumanoidProfileComponent>(ent))
                continue;
            if (!entMan.TryGetComponent<MobStateComponent>(ent, out var state))
                continue;
            if (state.CurrentState != MobState.Dead)
                continue;
            if (entMan.HasComponent<GhoulComponent>(ent))
                continue;
            if (entMan.HasComponent<ShatteredRisenComponent>(ent))
                continue;
            if (!mindSys.TryGetMind(ent, out _, out _))
                continue;
            _corpse = ent;
            break;
        }
        if (_corpse == EntityUid.Invalid)
        {
            outstr = Loc.GetString("heretic-ritual-fail-shattered-no-corpse");
            return false;
        }
        outstr = null;
        return true;
    }
    public override void Finalize(RitualData args)
    {
        var entMan = args.EntityManager;
        if (_corpse == EntityUid.Invalid || !entMan.EntityExists(_corpse))
            return;
        if (args.Limited != null && args.Limit > 0 && args.Limited.Count >= args.Limit)
            return;

        var coords = entMan.GetComponent<TransformComponent>(_corpse).Coordinates;
        var risen = entMan.SpawnEntity("MobShatteredRisen", coords);

        var mindSys = entMan.System<SharedMindSystem>();
        if (mindSys.TryGetMind(_corpse, out var mindId, out var mind))
        {
            mindSys.TransferTo(mindId, risen, mind: mind);
        }

        var minion = entMan.EnsureComponent<HereticMinionComponent>(risen);
        minion.BoundHeretic = args.Performer;

        var ghoulSys = entMan.System<GhoulSystem>();
        if (entMan.TryGetComponent<GhoulComponent>(risen, out var ghoul))
        {
            ghoul.TotalHealth = 350;
            ghoul.GiveBlade = false;
            ghoulSys.GhoulifyEntity((risen, ghoul));
        }

        entMan.EnsureComponent<ShatteredRisenComponent>(risen);

        if (args.Limited != null)
            args.Limited.Add(risen);

        entMan.QueueDeleteEntity(_corpse);
    }
}
