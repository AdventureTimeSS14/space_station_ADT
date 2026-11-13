using Content.Server.ADT.Salvage.Systems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.ADT.BloodMiner;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.ADT.BloodMiner;

public sealed class BloodMinerCombatSystem : EntitySystem
{
    [Dependency] private BloodMinerDashSystem _dash = default!;
    [Dependency] private BloodMinerSystem _miner = default!;
    [Dependency] private GunSystem _gun = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodMinerComponent, ADTMegafaunaOpenFireEvent>(OnOpenFire);
    }

    private void OnOpenFire(Entity<BloodMinerComponent> ent, ref ADTMegafaunaOpenFireEvent args)
    {
        OpenFire(ent, args.Target);
    }

    private void OpenFire(Entity<BloodMinerComponent> ent, EntityUid target)
    {
        var targetCoords = _transform.GetMapCoordinates(target);
        if (targetCoords.MapId == MapId.Nullspace)
            return;

        var origin = _transform.GetMapCoordinates(ent);
        if (origin.MapId != targetCoords.MapId)
            return;

        if ((targetCoords.Position - origin.Position).Length() > ent.Comp.DashRange)
            _dash.TryDash(ent, targetCoords);

        TryShoot(ent, target);
        _miner.TryTransformSaw(ent);
    }

    private bool TryShoot(Entity<BloodMinerComponent> ent, EntityUid target)
    {
        if (_timing.CurTime < ent.Comp.NextShotAt)
            return false;

        if (_mobState.IsDead(target))
            return false;

        var origin = _transform.GetMapCoordinates(ent);
        var targetCoords = _transform.GetMapCoordinates(target);
        if (origin.MapId != targetCoords.MapId)
            return false;

        var distance = (targetCoords.Position - origin.Position).Length();
        if (distance > ent.Comp.DashRange || distance < ent.Comp.MinShootDistance)
            return false;

        var gun = Spawn(ent.Comp.GunProto, Transform(ent).Coordinates);
        if (!TryComp<GunComponent>(gun, out var gunComp))
        {
            QueueDel(gun);
            return false;
        }

        var shot = _gun.AttemptShoot(ent.Owner, (gun, gunComp), Transform(target).Coordinates, target);
        QueueDel(gun);

        if (shot)
            ent.Comp.NextShotAt = _timing.CurTime + ent.Comp.RangedCooldown;

        return shot;
    }
}
