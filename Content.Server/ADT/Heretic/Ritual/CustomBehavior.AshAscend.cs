using Content.Server.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Damage.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Systems;
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

        burningUids = new();

        foreach (var uid in uids)
        {
            if (args.EntityManager.TryGetComponent<FlammableComponent>(uid, out var flam) && flam.OnFire)
                burningUids.Add(uid);
        }

        if (burningUids.Count < Min)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice-ash");
            return false;
        }

        outstr = null;
        return true;
    }

    public override void Finalize(RitualData args)
    {
        var damageableSystem = args.EntityManager.System<DamageableSystem>();
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        foreach (var uid in burningUids)
        {
            // YES!!! ASH!!!
            if (args.EntityManager.TryGetComponent<DamageableComponent>(uid, out var dmg))
            {
                var prot = (ProtoId<DamageGroupPrototype>)"Burn";
                var dmgtype = prototypeManager.Index(prot);
                damageableSystem.TryChangeDamage(uid, new DamageSpecifier(dmgtype, 3984f), true);
            }
        }

        uids = burningUids;
        base.Finalize(args);

        // reset it because blehhh
        burningUids = new();
    }
}
