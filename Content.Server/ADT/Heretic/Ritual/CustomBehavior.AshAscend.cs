//

using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

public sealed partial class RitualAshAscendBehavior : RitualSacrificeBehavior
{
    private List<EntityUid> burningUids = new();

    // check for burning corpses
    public override bool Execute(RitualData args, out string? outstr)
    {
        if (!base.Execute(args, out outstr))
            return false;

        // ADT: must be reset, otherwise stale uids from a failed attempt let the next one pass
        burningUids = new();

        foreach (var uid in uids)
        {
            if (!args.EntityManager.TryGetComponent<FlammableComponent>(uid, out var flam))
                continue;

            if (flam.OnFire)
                burningUids.Add(uid);

            if (burningUids.Count >= Max)
                break;
        }

        if (burningUids.Count < Min)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice-ash");
            return false;
        }

        outstr = null;
        return true;
    }
}
