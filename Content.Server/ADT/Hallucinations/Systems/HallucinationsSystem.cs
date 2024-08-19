using Robust.Shared.Audio.Systems;
using Content.Shared.Humanoid;
using Content.Shared.StatusEffect;
using Robust.Shared.Timing;
using Content.Shared.Database;
using Content.Shared.ADT.Hallucinations;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Speech;
using Content.Shared.Administration.Logs;
using Content.Shared.Eye;

namespace Content.Server.ADT.Hallucinations;

public sealed partial class HallucinationsSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public static string HallucinatingKey = "Hallucinations";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HallucinationsComponent, MapInitEvent>(OnHallucinationsInit);
        SubscribeLocalEvent<HallucinationsComponent, ComponentShutdown>(OnHallucinationsShutdown);
        SubscribeLocalEvent<HallucinationsDiseaseComponent, MapInitEvent>(OnHallucinationsDiseaseInit);
        SubscribeLocalEvent<HallucinationsDiseaseComponent, ComponentShutdown>(OnHallucinationsDiseaseShutdown);
        SubscribeLocalEvent<HallucinationsDiseaseComponent, EntitySpokeEvent>(OnEntitySpoke);

    }

    private void OnHallucinationsInit(EntityUid uid, HallucinationsComponent component, MapInitEvent args)
    {
        component.Layer = (ushort)(_random.Next(4, 32) << 1);
        if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
            return;
        UpdatePreset(component);
        _eye.SetVisibilityMask(uid, eye.VisibilityMask | component.Layer, eye);

        //_eye.SetVisibilityMask(uid, eye.VisibilityMask | component.Layer, eye);
        _adminLogger.Add(LogType.Action, LogImpact.Medium,
        $"{ToPrettyString(uid):player} began to hallucinate.");
    }

    private void OnHallucinationsDiseaseInit(EntityUid uid, HallucinationsDiseaseComponent component, MapInitEvent args)
    {
        component.Layer = (ushort)(_random.Next(4, 32) << 1);
        if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
            return;
        _eye.SetVisibilityMask(uid, eye.VisibilityMask | component.Layer, eye);

        //_eye.SetVisibilityMask(uid, eye.VisibilityMask | component.Layer, eye);
        _adminLogger.Add(LogType.Action, LogImpact.Medium,
        $"{ToPrettyString(uid):player} began to hallucinate.");
    }

    public void UpdatePreset(HallucinationsComponent component)
    {
        if (component.Proto == null)
            return;
        var preset = component.Proto;

        component.Spawns = preset.Entities;
        component.Range = preset.Range;
        component.SpawnRate = preset.SpawnRate;
        component.MinChance = preset.MinChance;
        component.MaxChance = preset.MaxChance;
        component.MaxSpawns = preset.MaxSpawns;
        component.IncreaseChance = preset.IncreaseChance;
    }
    private void OnHallucinationsShutdown(EntityUid uid, HallucinationsComponent component, ComponentShutdown args)
    {
        if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
            return;
        _eye.SetVisibilityMask(uid, eye.VisibilityMask & ~(ushort)component.Layer, eye);

        //_eye.SetVisibilityMask(uid, eye.VisibilityMask & ~component.Layer, eye);
        _adminLogger.Add(LogType.Action, LogImpact.Medium,
        $"{ToPrettyString(uid):player} stopped hallucinating.");
    }

    private void OnHallucinationsDiseaseShutdown(EntityUid uid, HallucinationsDiseaseComponent component, ComponentShutdown args)
    {
        if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
            return;
        _eye.SetVisibilityMask(uid, eye.VisibilityMask & ~(ushort)component.Layer, eye);

        //_eye.SetVisibilityMask(uid, eye.VisibilityMask & ~component.Layer, eye);
        _adminLogger.Add(LogType.Action, LogImpact.Medium,
        $"{ToPrettyString(uid):player} stopped hallucinating.");
    }

    /// <summary>
    ///     Attempts to start hallucinations for target
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="key">Status effect key.</param>
    /// <param name="time">Duration of hallucinations effect.</param>
    /// <param name="refresh">Refresh active effects.</param>
    /// <param name="proto">Hallucinations pack prototype.</param>
    public bool StartHallucinations(EntityUid target, string key, TimeSpan time, bool refresh, string proto)
    {
        if (proto == null)
            return false;
        if (!_proto.TryIndex<HallucinationsPrototype>(proto, out var prototype))
            return false;
        if (!_status.TryAddStatusEffect<HallucinationsComponent>(target, key, time, refresh))
            return false;

        var hallucinations = _entityManager.GetComponent<HallucinationsComponent>(target);
        hallucinations.Proto = prototype;
        UpdatePreset(hallucinations);
        hallucinations.CurChance = prototype.MinChance;

        return true;
    }

    /// <summary>
    ///     Attempts to start epidemic hallucinations. Spreads by speech
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="proto">Hallucinations pack prototype.</param>
    public bool StartEpidemicHallucinations(EntityUid target, string proto)
    {
        if (proto == null)
            return false;
        if (!_proto.TryIndex<HallucinationsPrototype>(proto, out var prototype))
            return false;

        var hallucinations = EnsureComp<HallucinationsDiseaseComponent>(target);
        hallucinations.EndTime = _timing.CurTime + TimeSpan.FromSeconds(15);

        hallucinations.Proto = prototype;
        hallucinations.Spawns = prototype.Entities;
        hallucinations.Range = prototype.Range;
        hallucinations.SpawnRate = prototype.SpawnRate;
        hallucinations.MinChance = prototype.MinChance;
        hallucinations.MaxChance = prototype.MaxChance;
        hallucinations.MaxSpawns = prototype.MaxSpawns;
        hallucinations.IncreaseChance = prototype.IncreaseChance;
        hallucinations.CurChance = prototype.MinChance;

        return true;
    }

    private void OnEntitySpoke(EntityUid uid, HallucinationsDiseaseComponent component, EntitySpokeEvent args)
    {
        if (component.Proto == null)
            return;

        foreach (var ent in _lookup.GetEntitiesInRange(uid, 7f))
        {
            if (!HasComp<HumanoidAppearanceComponent>(ent))
                continue;
            StartEpidemicHallucinations(ent, component.Proto.ID);
        }
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HallucinationsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var stat, out var xform))
        {
            if (_timing.CurTime < stat.NextSecond)
                continue;
            var rate = stat.SpawnRate;
            stat.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(rate);

            if (stat.CurChance < stat.MaxChance && stat.CurChance + stat.IncreaseChance <= 1)
                stat.CurChance = stat.CurChance + stat.IncreaseChance;

            if (!_random.Prob(stat.CurChance))
                continue;

            stat.SpawnedCount = 0;

            var range = stat.Range * 4;
            UpdatePreset(stat);

            foreach (var (ent, comp) in _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(xform.MapPosition, range))
            {
                var newCoords = Transform(ent).MapPosition.Offset(_random.NextVector2(stat.Range));

                if (stat.SpawnedCount >= stat.MaxSpawns)
                    continue;
                stat.SpawnedCount = stat.SpawnedCount += 1;

                var hallucination = Spawn(stat.Spawns[_random.Next(0, stat.Spawns.Count - 1)], newCoords);
                EnsureComp<VisibilityComponent>(hallucination, out var visibility);
                _visibilitySystem.SetLayer((hallucination, visibility), (ushort)stat.Layer, false);
                _visibilitySystem.RefreshVisibility(hallucination, visibilityComponent: visibility);

                //_visibilitySystem.SetLayer((hallucination, visibility), (ushort) stat.Layer, false);
                //_visibilitySystem.RefreshVisibility(hallucination, visibilityComponent: visibility);
            }

            var uidnewCoords = Transform(uid).MapPosition.Offset(_random.NextVector2(stat.Range));
            if (stat.SpawnedCount >= stat.MaxSpawns)
                continue;
            stat.SpawnedCount = stat.SpawnedCount += 1;

            var uidhallucination = Spawn(stat.Spawns[_random.Next(0, stat.Spawns.Count - 1)], uidnewCoords);
            EnsureComp<VisibilityComponent>(uidhallucination, out var uidvisibility);
            _visibilitySystem.SetLayer((uidhallucination, uidvisibility), (ushort)stat.Layer, false);
            _visibilitySystem.RefreshVisibility(uidhallucination, visibilityComponent: uidvisibility);

            //_visibilitySystem.SetLayer((uidhallucination, uidvisibility), (ushort) stat.Layer, false);
            //_visibilitySystem.RefreshVisibility(uidhallucination, visibilityComponent: uidvisibility);

        }

        var diseaseQuery = EntityQueryEnumerator<HallucinationsDiseaseComponent, TransformComponent>();
        while (diseaseQuery.MoveNext(out var uid, out var stat, out var xform))
        {
            if (_timing.CurTime >= stat.EndTime)
            {
                RemCompDeferred<HallucinationsDiseaseComponent>(uid);
                continue;
            }

            if (_timing.CurTime < stat.NextSecond)
                continue;
            var rate = stat.SpawnRate;
            stat.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(rate);

            if (stat.CurChance < stat.MaxChance && stat.CurChance + stat.IncreaseChance <= 1)
                stat.CurChance = stat.CurChance + stat.IncreaseChance;

            if (!_random.Prob(stat.CurChance))
                continue;

            stat.SpawnedCount = 0;

            var range = stat.Range * 4;

            foreach (var (ent, comp) in _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(xform.MapPosition, range))
            {
                var newCoords = Transform(ent).MapPosition.Offset(_random.NextVector2(stat.Range));

                if (stat.SpawnedCount >= stat.MaxSpawns)
                    continue;
                stat.SpawnedCount = stat.SpawnedCount += 1;

                var hallucination = Spawn(stat.Spawns[_random.Next(0, stat.Spawns.Count - 1)], newCoords);
                EnsureComp<VisibilityComponent>(hallucination, out var visibility);
                _visibilitySystem.SetLayer((hallucination, visibility), (ushort)stat.Layer, false);
                _visibilitySystem.RefreshVisibility(hallucination, visibilityComponent: visibility);

                //_visibilitySystem.SetLayer((hallucination, visibility), (ushort)stat.Layer, false);
                //_visibilitySystem.RefreshVisibility(hallucination, visibilityComponent: visibility);
            }

            var uidnewCoords = Transform(uid).MapPosition.Offset(_random.NextVector2(stat.Range));
            if (stat.SpawnedCount >= stat.MaxSpawns)
                continue;
            stat.SpawnedCount = stat.SpawnedCount += 1;

            var uidhallucination = Spawn(stat.Spawns[_random.Next(0, stat.Spawns.Count - 1)], uidnewCoords);
            EnsureComp<VisibilityComponent>(uidhallucination, out var uidvisibility);
            _visibilitySystem.SetLayer((uidhallucination, uidvisibility), (ushort)stat.Layer, false);
            _visibilitySystem.RefreshVisibility(uidhallucination, visibilityComponent: uidvisibility);

            //_visibilitySystem.SetLayer((uidhallucination, uidvisibility), (ushort) stat.Layer, false);
            //_visibilitySystem.RefreshVisibility(uidhallucination, visibilityComponent: uidvisibility);

        }
    }

}
