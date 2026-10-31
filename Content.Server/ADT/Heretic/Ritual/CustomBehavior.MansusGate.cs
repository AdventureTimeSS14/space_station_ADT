using Content.Shared.ADT.Heretic.Components.Side;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Storage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
namespace Content.Server.ADT.Heretic.Ritual;
public sealed partial class RitualMansusGateBehavior : RitualCustomBehavior
{
    private EntityUid _locker;
    public override bool Execute(RitualData args, out string? outstr)
    {
        _locker = EntityUid.Invalid;
        var entMan = args.EntityManager;
        var lookup = entMan.System<EntityLookupSystem>();
        foreach (var ent in lookup.GetEntitiesInRange(args.Platform, 1.5f))
        {
            if (ent == args.Performer)
                continue;
            if (!entMan.HasComponent<EntityStorageComponent>(ent))
                continue;
            _locker = ent;
            break;
        }
        if (_locker == EntityUid.Invalid)
        {
            outstr = Loc.GetString("heretic-ritual-fail-gate-no-locker");
            return false;
        }
        if (args.Limited != null && args.Limit > 0 && args.Limited.Count >= args.Limit)
        {
            outstr = Loc.GetString("heretic-ritual-fail-limit");
            return false;
        }
        outstr = null;
        return true;
    }
    public override void Finalize(RitualData args)
    {
        var entMan = args.EntityManager;
        if (_locker == EntityUid.Invalid || !entMan.EntityExists(_locker))
            return;
        if (args.Limited != null && args.Limit > 0 && args.Limited.Count >= args.Limit)
            return;
        var entMan2 = args.EntityManager;
        var gateA = entMan2.SpawnEntity("HereticMansusGate", entMan2.GetComponent<TransformComponent>(args.Platform).Coordinates);
        var gateB = entMan2.SpawnEntity("HereticMansusGate", entMan2.GetComponent<TransformComponent>(args.Performer).Coordinates);
        if (!entMan2.TryGetComponent<TransformComponent>(gateB, out var gateBXform))
            return;
        gateBXform.LocalPosition += new System.Numerics.Vector2(1.5f, 0f);
        entMan2.Dirty(gateB, gateBXform);
        var compA = entMan2.EnsureComponent<HereticMansusGateComponent>(gateA);
        var compB = entMan2.EnsureComponent<HereticMansusGateComponent>(gateB);
        compA.LinkedGate = gateB;
        compB.LinkedGate = gateA;
        entMan2.Dirty(gateA, compA);
        entMan2.Dirty(gateB, compB);
        if (args.Limited != null)
            args.Limited.Add(gateA);
    }
}
