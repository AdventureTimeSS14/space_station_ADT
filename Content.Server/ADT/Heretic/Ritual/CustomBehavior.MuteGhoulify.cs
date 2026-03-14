using System.Linq;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Speech.Muting;

namespace Content.Server.Heretic.Ritual;

public sealed partial class RitualMuteGhoulifyBehavior : RitualCustomBehavior
{
    /// <summary>
    ///     Minimal amount of corpses.
    /// </summary>
    [DataField]
    public int Min = 1;

    /// <summary>
    ///     Maximum amount of corpses.
    /// </summary>
    [DataField]
    public int Max = 1;

    private List<EntityUid> _targets = new();

    public override bool Execute(RitualData args, out string? outstr)
    {
        var lookupSystem = args.EntityManager.System<EntityLookupSystem>();

        _targets = new();

        if (!args.EntityManager.TryGetComponent<HereticComponent>(args.Performer, out var hereticComp))
        {
            outstr = string.Empty;
            return false;
        }

        var res = lookupSystem.GetEntitiesInRange(args.Platform, .75f);
        if (res.Count == 0)
        {
            outstr = Loc.GetString("heretic-ritual-fail-ghoulify");
            return false;
        }

        // get all the dead ones
        foreach (var look in res)
        {
            if (!args.EntityManager.TryGetComponent<MobStateComponent>(look, out var mobstate)
                || !args.EntityManager.HasComponent<HumanoidAppearanceComponent>(look)
                || mobstate.CurrentState != Shared.Mobs.MobState.Dead)
                continue;

            _targets.Add(look);
        }

        if (_targets.Count < Min)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice-ineligible");
            return false;
        }

        outstr = null;
        return true;
    }

    public override void Finalize(RitualData args)
    {
        foreach (var uid in _targets.Take(Max))
        {
            var ghoul = new GhoulComponent()
            {
                TotalHealth = 100,
            };
            args.EntityManager.AddComponent(uid, ghoul, overwrite: true);
            args.EntityManager.EnsureComponent<MutedComponent>(uid);
        }
    }
}
