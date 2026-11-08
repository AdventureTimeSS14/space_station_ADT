using Content.Server.Electrocution;
using Content.Server.Lightning;
using Content.Server.Power.Components;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.ADT.Vetus;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Emp;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Wall;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.ADT.Vetus;

public sealed partial class ADTVetusSystem
{
    [Dependency] private ElectrocutionSystem _electrocution = default!;
    [Dependency] private LightningSystem _lightning = default!;
    [Dependency] private SharedEmpSystem _emp = default!;

    private static readonly TimeSpan ShockedTime = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MachineEmpDuration = TimeSpan.FromSeconds(15);
    private const float MachineEmpEnergy = 10000f;

    private readonly Dictionary<EntityUid, TimeSpan> _beingShocked = new();
    private readonly HashSet<EntityUid> _zapCandidates = new();
    private readonly List<EntityUid> _shockedCleanup = new();

    private enum ZapType : byte
    {
        None,
        Structure,
        Machinery,
        Living,
    }

    private void InitializeTesla()
    {
    }

    private void UpdateTesla(TimeSpan now)
    {
        if (_beingShocked.Count > 0)
        {
            _shockedCleanup.Clear();

            foreach (var (uid, until) in _beingShocked)
            {
                if (now >= until)
                    _shockedCleanup.Add(uid);
            }

            foreach (var uid in _shockedCleanup)
            {
                _beingShocked.Remove(uid);
            }
        }

        var query = EntityQueryEnumerator<ADTVetusTeslaBoltComponent>();

        while (query.MoveNext(out var uid, out var bolt))
        {
            TeslaZap(uid, bolt, bolt.ZapRange, bolt.Power, new HashSet<EntityUid>());
        }
    }

    private bool IsBeingShocked(EntityUid uid)
    {
        return _beingShocked.TryGetValue(uid, out var until) && _timing.CurTime < until;
    }

    private void SetBeingShocked(EntityUid uid)
    {
        _beingShocked[uid] = _timing.CurTime + ShockedTime;
    }

    private void TeslaZap(EntityUid source, ADTVetusTeslaBoltComponent bolt, int zapRange, float power, HashSet<EntityUid> shocked)
    {
        if (TerminatingOrDeleted(source))
            return;

        shocked.Add(source);

        if (power < bolt.Cutoff)
            return;

        var center = _transform.GetMapCoordinates(source);

        if (center.MapId == MapId.Nullspace)
            return;

        var searchRange = zapRange + 2;
        EntityUid? closest = null;
        var closestType = ZapType.None;
        var closestDistance = float.MaxValue;

        _zapCandidates.Clear();
        _lookup.GetEntitiesInRange(center.MapId, center.Position, searchRange * MathF.Sqrt(2f) + 0.5f, _zapCandidates);

        foreach (var candidate in _zapCandidates)
        {
            if (shocked.Contains(candidate) || IsBeingShocked(candidate))
                continue;

            var type = GetZapType(candidate);

            if (type == ZapType.None || type < closestType)
                continue;

            var coords = _transform.GetMapCoordinates(candidate);
            var delta = coords.Position - center.Position;

            if (MathF.Max(MathF.Abs(delta.X), MathF.Abs(delta.Y)) > searchRange + 0.5f)
                continue;

            var distance = delta.Length();

            if (type == closestType && distance >= closestDistance)
                continue;

            if (!_examine.InRangeUnOccluded(center, coords, searchRange * MathF.Sqrt(2f) + 0.5f, null))
                continue;

            closest = candidate;
            closestType = type;
            closestDistance = distance;
        }

        if (closest is not { } target)
            return;

        _lightning.ShootLightning(source, target, bolt.ArcPrototype, false);

        var nextRange = 2;

        switch (closestType)
        {
            case ZapType.Living:
                SetBeingShocked(target);

                var shockDamage = Math.Min((int) MathF.Round(power / bolt.PowerPerDamage), bolt.MaxDamage)
                                  + _random.Next(-bolt.DamageSpread, bolt.DamageSpread + 1);

                if (shockDamage > 0)
                    _electrocution.TryDoElectrocution(target, source, shockDamage, TimeSpan.Zero, true);

                nextRange = 5;
                power /= 1.5f;
                break;
            case ZapType.Machinery:
                SetBeingShocked(target);

                var machineDamage = new DamageSpecifier();
                machineDamage.DamageDict.Add("Heat", power * bolt.MachineDamagePerPower);
                _damageable.TryChangeDamage(target, machineDamage, origin: source);

                if (_random.Prob(bolt.MachineEmpChance))
                    _emp.TryEmpEffects(target, MachineEmpEnergy, MachineEmpDuration);

                power *= 0.5f;
                break;
            case ZapType.Structure:
                SetBeingShocked(target);
                power *= 0.5f;
                break;
        }

        if (_random.Prob(bolt.ForkChance))
        {
            var copy = new HashSet<EntityUid>(shocked);
            TeslaZap(target, bolt, nextRange, power * 0.5f, copy);
            TeslaZap(target, bolt, nextRange, power * 0.5f, shocked);
            shocked.UnionWith(copy);
            return;
        }

        TeslaZap(target, bolt, nextRange, power, shocked);
    }

    private ZapType GetZapType(EntityUid uid)
    {
        if (HasComp<ADTVetusLegComponent>(uid))
            return ZapType.Living;

        if (HasComp<MobStateComponent>(uid))
            return _mobState.IsDead(uid) ? ZapType.None : ZapType.Living;

        if (HasComp<ApcPowerReceiverComponent>(uid))
            return ZapType.Machinery;

        if (!Transform(uid).Anchored || !HasComp<DamageableComponent>(uid))
            return ZapType.None;

        if (HasComp<ItemComponent>(uid) || HasComp<WallComponent>(uid) || HasComp<ADTMineralComponent>(uid))
            return ZapType.None;

        return ZapType.Structure;
    }
}
