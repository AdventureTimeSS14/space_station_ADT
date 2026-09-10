using System.Numerics;
using System.Threading;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Mech.Systems;
using Content.Shared.ADT.Weapons.Medbeam;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mech.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using RobustTimer = Robust.Shared.Timing.Timer;

namespace Content.Server.ADT.Weapons.Medbeam;

public sealed class ADTMedbeamSystem : SharedADTMedbeamSystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedBloodstreamSystem _blood = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private readonly Dictionary<EntityUid, CancellationTokenSource> _beamTokens = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTMedbeamComponent, ComponentShutdown>(OnShutdown);
    }

    public override void AttachBeam(Entity<ADTMedbeamComponent> ent, EntityUid target)
    {
        base.AttachBeam(ent, target);

        if (_beamTokens.Remove(ent.Owner, out var old))
            old.Cancel();

        var cts = new CancellationTokenSource();
        _beamTokens[ent.Owner] = cts;
        RobustTimer.SpawnRepeating(TimeSpan.FromSeconds(ent.Comp.UpdateInterval), () => OnBeamTick(ent, cts), cts.Token);
    }

    public override void DetachBeam(Entity<ADTMedbeamComponent> ent)
    {
        if (_beamTokens.Remove(ent.Owner, out var cts))
            cts.Cancel();

        base.DetachBeam(ent);
    }

    private void OnShutdown(Entity<ADTMedbeamComponent> ent, ref ComponentShutdown args)
    {
        if (_beamTokens.Remove(ent.Owner, out var cts))
            cts.Cancel();
    }

    private void OnBeamTick(Entity<ADTMedbeamComponent> ent, CancellationTokenSource cts)
    {
        if (cts.IsCancellationRequested)
            return;

        if (!Exists(ent.Owner) || ent.Comp.Target == null)
        {
            cts.Cancel();
            if (_beamTokens.TryGetValue(ent.Owner, out var current) && current == cts)
                _beamTokens.Remove(ent.Owner);
            return;
        }

        TickBeam(ent);
    }

    private void TickBeam(Entity<ADTMedbeamComponent> ent)
    {
        var target = ent.Comp.Target;
        if (!Exists(target))
        {
            DetachBeam(ent);
            return;
        }

        if (GetHolder(ent) is not { } holder)
        {
            DetachBeam(ent);
            return;
        }

        var hasSomethingToHeal = HasSomethingToHeal(ent, target.Value);

        if (TryComp<MechComponent>(holder, out var mech))
        {
            if (mech.PilotSlot.ContainedEntity is not { } pilot || !_mobState.IsAlive(pilot))
            {
                DetachBeam(ent);
                return;
            }

            if (hasSomethingToHeal)
            {
                var energyUsage = (FixedPoint2) (ent.Comp.EnergyUsage * ent.Comp.UpdateInterval);
                if (mech.Energy < energyUsage)
                {
                    DetachBeam(ent);
                    return;
                }

                _mech.TryChangeEnergy(holder, -energyUsage, mech);
            }
        }
        else if (ent.Comp.RequireMech || !_mobState.IsAlive(holder))
        {
            DetachBeam(ent);
            return;
        }

        if (!_examine.InRangeUnOccluded(ent.Owner, target.Value, ent.Comp.MaxRange,
                entity => entity == ent.Owner || entity == target.Value))
        {
            DetachBeam(ent);
            return;
        }

        if (TryGetCrossing(ent, target.Value, out var otherGun, out var epicenter))
        {
            ExplodeBeams(ent, (otherGun, Comp<ADTMedbeamComponent>(otherGun)), epicenter);
            return;
        }

        if (!hasSomethingToHeal)
            return;

        _damage.TryChangeDamage(target.Value, ent.Comp.Damage, origin: ent.Owner);

        if (HasComp<BloodstreamComponent>(target.Value))
        {
            if (ent.Comp.BloodRestore > 0)
                _blood.TryRegenerateBlood(target.Value, (FixedPoint2) ent.Comp.BloodRestore);

            _blood.TryModifyBleedAmount(target.Value, -Comp<BloodstreamComponent>(target.Value).BleedAmount);
        }
    }

    private bool HasSomethingToHeal(Entity<ADTMedbeamComponent> ent, EntityUid target)
    {
        if (TryComp<DamageableComponent>(target, out var damageable) && damageable.TotalDamage > 0)
            return true;

        if (HasComp<BloodstreamComponent>(target))
        {
            if (Comp<BloodstreamComponent>(target).BleedAmount > 0)
                return true;

            if (ent.Comp.BloodRestore > 0 && _blood.GetBloodLevel(target) < 1f)
                return true;
        }

        return false;
    }

    private bool TryGetCrossing(Entity<ADTMedbeamComponent> ent, EntityUid target, out EntityUid otherGun, out MapCoordinates epicenter)
    {
        otherGun = default;
        epicenter = default;

        var gunCoords = _xform.GetMapCoordinates(ent.Owner);
        var targetPos = _xform.GetMapCoordinates(target).Position;

        var query = EntityQueryEnumerator<ADTMedbeamComponent>();
        while (query.MoveNext(out var uid, out var other))
        {
            if (uid == ent.Owner || other.Target == null)
                continue;

            var otherCoords = _xform.GetMapCoordinates(uid);
            var otherTargetCoords = _xform.GetMapCoordinates(other.Target.Value);
            if (otherCoords.MapId != gunCoords.MapId || otherTargetCoords.MapId != gunCoords.MapId)
                continue;

            if (!TrySegmentIntersect(gunCoords.Position, targetPos, otherCoords.Position, otherTargetCoords.Position, out var point))
                continue;

            otherGun = uid;
            epicenter = new MapCoordinates(point, gunCoords.MapId);
            return true;
        }

        return false;
    }

    private void ExplodeBeams(Entity<ADTMedbeamComponent> a, Entity<ADTMedbeamComponent> b, MapCoordinates epicenter)
    {
        a.Comp.Target = null;
        a.Comp.Accumulator = 0;
        b.Comp.Target = null;
        b.Comp.Accumulator = 0;
        Dirty(a.Owner, a.Comp);
        Dirty(b.Owner, b.Comp);

        var explosionType = a.Comp.ExplosionType;
        var totalIntensity = a.Comp.ExplosionTotalIntensity;
        var slope = a.Comp.ExplosionIntensitySlope;
        var maxTileIntensity = a.Comp.ExplosionMaxTileIntensity;

        _explosion.QueueExplosion(epicenter, explosionType, totalIntensity, slope, maxTileIntensity, cause: a.Owner);
        _explosion.QueueExplosion(_xform.GetMapCoordinates(a.Owner), explosionType, totalIntensity, slope, maxTileIntensity, cause: a.Owner);
        _explosion.QueueExplosion(_xform.GetMapCoordinates(b.Owner), explosionType, totalIntensity, slope, maxTileIntensity, cause: b.Owner);

        QueueDel(a.Owner);
        QueueDel(b.Owner);
    }

    private static bool TrySegmentIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 point)
    {
        point = default;

        var r = a2 - a1;
        var s = b2 - b1;
        var rxs = Cross(r, s);
        var qmp = b1 - a1;

        if (MathHelper.CloseTo(rxs, 0))
        {
            if (!MathHelper.CloseTo(Cross(qmp, r), 0))
                return false;

            var denom = Vector2.Dot(r, r);
            if (MathHelper.CloseTo(denom, 0))
                return false;

            var t0 = Vector2.Dot(qmp, r) / denom;
            var t1 = t0 + Vector2.Dot(s, r) / denom;
            var minT = MathF.Min(t0, t1);
            var maxT = MathF.Max(t0, t1);

            if (maxT < 0 || minT > 1)
                return false;

            point = a1 + r * Math.Clamp((minT + maxT) / 2, 0, 1);
            return true;
        }

        var t = Cross(qmp, s) / rxs;
        var u = Cross(qmp, r) / rxs;

        if (t < 0 || t > 1 || u < 0 || u > 1)
            return false;

        point = a1 + r * t;
        return true;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }
}