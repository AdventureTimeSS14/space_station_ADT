using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Speech.Muting;

namespace Content.Server.Heretic.Ritual;

public sealed partial class RitualMuteGhoulifyBehavior : RitualSacrificeBehavior
{
    public override bool Execute(RitualData args, out string? outstr)
    {
        var lookupSystem = args.EntityManager.System<EntityLookupSystem>();

        uids = new();

        if (!args.EntityManager.TryGetComponent<HereticComponent>(args.Performer, out var hereticComp))
        {
            outstr = string.Empty;
            return false;
        }

        var res = lookupSystem.GetEntitiesInRange(args.Platform, 1.5f);
        if (res.Count == 0)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice");
            return false;
        }

        foreach (var look in res)
        {
            if (!args.EntityManager.TryGetComponent<MobStateComponent>(look, out var mobstate)
                || !args.EntityManager.HasComponent<HumanoidAppearanceComponent>(look)
                || mobstate.CurrentState != Shared.Mobs.MobState.Dead)
                continue;

            uids.Add(look);
        }

        if (uids.Count < Min)
        {
            var needed = (int)Min - uids.Count;
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice-count", ("current", uids.Count), ("required", (int)Min), ("needed", needed), ("max", (int)Max));
            return false;
        }

        outstr = null;
        return true;
    }

    public override void Finalize(RitualData args)
    {
        foreach (var uid in uids)
        {
            var ghoul = new GhoulComponent()
            {
                TotalHealth = 125f,
            };
            args.EntityManager.AddComponent(uid, ghoul, overwrite: true);
            args.EntityManager.EnsureComponent<MutedComponent>(uid);
        }
    }
}