using System.Diagnostics.CodeAnalysis;
using Content.Shared.ADT.Heretic.Common;
using Content.Shared.ADT.Heretic;
using Content.Shared.Heretic;
using Content.Shared.Mind;
using Content.Shared.Damage.Systems;

namespace Content.Shared.ADT.Heretic.Systems;

public abstract class SharedHereticSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    private EntityQuery<HereticComponent> _hereticQuery;
    private EntityQuery<GhoulComponent> _ghoulQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCheckEvent>(OnCheck);
        SubscribeLocalEvent<BeforeConversionEvent>(OnBeforeConversion);

        _hereticQuery = GetEntityQuery<HereticComponent>();
        _ghoulQuery = GetEntityQuery<GhoulComponent>();
    }

    private void OnBeforeConversion(ref BeforeConversionEvent ev)
    {
        if (TryGetHereticComponent(ev.Uid, out _, out _))
            ev.Blocked = true;
    }

    private void OnCheck(ref HereticCheckEvent ev)
    {
        ev.Result = TryGetHereticComponent(ev.Uid, out _, out _);
    }

    public bool TryGetHereticComponent(
        EntityUid uid,
        [NotNullWhen(true)] out HereticComponent? heretic,
        out EntityUid mind)
    {
        heretic = null;
        return _mind.TryGetMind(uid, out mind, out _) && _hereticQuery.TryComp(mind, out heretic);
    }

    public bool IsHereticOrGhoul(EntityUid uid)
    {
        return _ghoulQuery.HasComp(uid) || TryGetHereticComponent(uid, out _, out _);
    }

    public float GetCellularDamage(EntityUid uid)
    {
        var spec = _damageable.GetAllDamage((uid, null));

        return spec.DamageDict.TryGetValue("Cellular", out var cellular)
            ? cellular.Float()
            : 0f;
    }
}
