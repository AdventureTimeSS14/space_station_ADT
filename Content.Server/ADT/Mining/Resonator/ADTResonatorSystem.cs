using Content.Server.Atmos.EntitySystems;
using Content.Server.Gatherable;
using Content.Server.Gatherable.Components;
using Content.Shared.ADT.Mining;
using Content.Shared.ADT.Mining.Resonator;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.StepTrigger.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Mining.Resonator;

public sealed class ADTResonatorSystem : SharedADTResonatorSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GatherableSystem _gatherable = default!;
    [Dependency] private readonly ADTHardRockSystem _hardRock = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StepTriggerSystem _stepTrigger = default!;

    private readonly List<Entity<ADTResonanceFieldComponent>> _dueFields = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTResonatorComponent, AfterInteractEvent>(OnAfterInteract);

        SubscribeLocalEvent<ADTResonanceFieldComponent, ComponentShutdown>(OnFieldShutdown);
        SubscribeLocalEvent<ADTResonanceFieldComponent, StepTriggerAttemptEvent>(OnFieldStepAttempt);
        SubscribeLocalEvent<ADTResonanceFieldComponent, StepTriggeredOffEvent>(OnFieldStepped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        _dueFields.Clear();

        var query = EntityQueryEnumerator<ADTResonanceFieldComponent>();
        while (query.MoveNext(out var uid, out var field))
        {
            if (field.BurstTime is not { } burstTime || now < burstTime)
                continue;

            _dueFields.Add((uid, field));
        }

        foreach (var due in _dueFields)
        {
            if (!due.Comp.Bursting && !TerminatingOrDeleted(due))
                Burst(due);
        }

        _dueFields.Clear();
    }

    private void OnAfterInteract(Entity<ADTResonatorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        args.Handled = TryPlant(ent, args.ClickLocation, args.User);
    }

    public bool TryPlant(Entity<ADTResonatorComponent> ent, EntityCoordinates coords, EntityUid user)
    {
        if (_timing.CurTime < ent.Comp.NextPlant)
            return false;

        if (SnapToTile(coords) is not { } target)
            return false;

        if (FindFieldAt(target) is { } existing)
        {
            existing.Comp.DamageMultiplier = ent.Comp.QuickBurstModifier;
            Burst(existing);
            StartPlantDelay(ent);
            return true;
        }

        ent.Comp.Fields.RemoveWhere(field => TerminatingOrDeleted(field));
        if (ent.Comp.Fields.Count >= ent.Comp.FieldLimit)
        {
            _popup.PopupEntity(Loc.GetString("adt-resonator-popup-limit"), ent, user);
            return false;
        }

        if (!TrySpendCharge(ent))
        {
            _popup.PopupEntity(Loc.GetString("adt-resonator-popup-no-charges"), ent, user);
            return false;
        }

        var field = Spawn(ent.Comp.FieldProto, target);
        SetupField(field, ent, user);
        ent.Comp.Fields.Add(field);

        _audio.PlayPvs(ent.Comp.PlantSound, field);
        StartPlantDelay(ent);

        return true;
    }

    public void BlastAt(EntProtoId proto, EntityCoordinates coords, EntityUid? user, float burstMultiplier)
    {
        if (SnapToTile(coords) is not { } target)
            return;

        if (FindFieldAt(target) is { } existing)
        {
            existing.Comp.DamageMultiplier = burstMultiplier;
            Burst(existing);
            return;
        }

        if (HasRockAt(target))
            return;

        var field = Spawn(proto, target);
        if (!TryComp<ADTResonanceFieldComponent>(field, out var comp))
            return;

        comp.Creator = user;
        comp.Mode = ADTResonatorMode.Manual;
        comp.FailureProbability = 1f;
        comp.BurstTime = _timing.CurTime + comp.Lifetime;
        _appearance.SetData(field, ADTResonanceFieldVisuals.Mode, comp.Mode);
        Dirty(field, comp);
    }

    private void StartPlantDelay(Entity<ADTResonatorComponent> ent)
    {
        ent.Comp.NextPlant = _timing.CurTime + ent.Comp.PlantDelay;
        Dirty(ent);
    }

    private bool TrySpendCharge(Entity<ADTResonatorComponent> ent)
    {
        if (!TryComp<BatteryComponent>(ent, out var battery))
            return true;

        return _battery.TryUseCharge((ent.Owner, battery), battery.MaxCharge * ent.Comp.ChargeUsePercent);
    }

    private void SetupField(EntityUid uid, Entity<ADTResonatorComponent> resonator, EntityUid user, float failure = 0f)
    {
        if (!TryComp<ADTResonanceFieldComponent>(uid, out var field))
            return;

        field.Creator = user;
        field.Resonator = resonator.Owner;
        field.Mode = resonator.Comp.Mode;
        field.AddedFailure = resonator.Comp.AddedFailure;
        field.FailureProbability = failure;

        field.BurstTime = _timing.CurTime + (field.Mode == ADTResonatorMode.Auto
            ? field.AutoDelay
            : field.Lifetime);

        if (TryComp<StepTriggerComponent>(uid, out var step))
            _stepTrigger.SetActive(uid, field.Mode == ADTResonatorMode.Matrix, step);

        _appearance.SetData(uid, ADTResonanceFieldVisuals.Mode, field.Mode);
        Dirty(uid, field);
    }

    private void OnFieldShutdown(Entity<ADTResonanceFieldComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<ADTResonatorComponent>(ent.Comp.Resonator, out var resonator))
            resonator.Fields.Remove(ent);
    }

    private void OnFieldStepAttempt(Entity<ADTResonanceFieldComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue |= ent.Comp.Mode == ADTResonatorMode.Matrix;
    }

    private void OnFieldStepped(Entity<ADTResonanceFieldComponent> ent, ref StepTriggeredOffEvent args)
    {
        if (ent.Comp.Mode == ADTResonatorMode.Matrix)
            Burst(ent);
    }

    public void Burst(Entity<ADTResonanceFieldComponent> ent)
    {
        if (ent.Comp.Bursting || TerminatingOrDeleted(ent))
            return;

        ent.Comp.Bursting = true;

        var coords = _transform.GetMapCoordinates(ent.Owner);

        if (ent.Comp.BurstEffect is { } effect)
            Spawn(effect, coords);

        _audio.PlayPvs(ent.Comp.BurstSound, Transform(ent).Coordinates);

        MineRock(ent, coords);
        HurtMobs(ent, coords);
        BurstNeighbours(ent, coords);
        Spread(ent, coords);

        QueueDel(ent);
    }

    private void MineRock(Entity<ADTResonanceFieldComponent> ent, MapCoordinates coords)
    {
        var rocks = new HashSet<Entity<GatherableComponent>>();
        _lookup.GetEntitiesInRange(coords, 0.1f, rocks);

        foreach (var rock in rocks)
        {
            if (!TerminatingOrDeleted(rock) && !_hardRock.IsHardRock(rock))
                _gatherable.Gather(rock, ent.Comp.Creator, rock.Comp);
        }
    }

    private void HurtMobs(Entity<ADTResonanceFieldComponent> ent, MapCoordinates coords)
    {
        var damage = ent.Comp.Damage * ent.Comp.DamageMultiplier;

        var pressure = _atmosphere.GetContainingMixture(ent.Owner)?.Pressure ?? 0f;
        if (pressure <= ent.Comp.LowPressureThreshold)
            damage *= ent.Comp.LowPressureMultiplier;

        var mobs = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(coords, 0.4f, mobs);

        foreach (var mob in mobs)
        {
            _popup.PopupEntity(Loc.GetString("adt-resonance-field-popup-ruptured"), mob, mob, PopupType.LargeCaution);
            _damageable.TryChangeDamage(mob.Owner, damage, origin: ent.Comp.Creator);

            if (ent.Comp.Creator is { } creator)
                _adminLog.Add(LogType.Damaged, LogImpact.Medium, $"{ToPrettyString(creator):user} ruptured a resonance field on {ToPrettyString(mob):target}");
        }
    }

    private void BurstNeighbours(Entity<ADTResonanceFieldComponent> ent, MapCoordinates coords)
    {
        if (ent.Comp.Mode == ADTResonatorMode.Matrix)
            return;

        var fields = new HashSet<Entity<ADTResonanceFieldComponent>>();
        _lookup.GetEntitiesInRange(coords, ent.Comp.SpreadRange, fields);

        foreach (var field in fields)
        {
            if (field.Owner != ent.Owner && !field.Comp.Bursting)
                Burst(field);
        }
    }

    private void Spread(Entity<ADTResonanceFieldComponent> ent, MapCoordinates coords)
    {
        if (ent.Comp.Resonator is not { } resonatorUid ||
            !TryComp<ADTResonatorComponent>(resonatorUid, out var resonator) ||
            _random.Prob(ent.Comp.FailureProbability))
        {
            return;
        }

        var rocks = new HashSet<Entity<GatherableComponent>>();
        _lookup.GetEntitiesInRange(coords, ent.Comp.SpreadRange, rocks);

        foreach (var rock in rocks)
        {
            if (TerminatingOrDeleted(rock) || _hardRock.IsHardRock(rock))
                continue;

            if (SnapToTile(Transform(rock).Coordinates) is not { } target || FindFieldAt(target) != null)
                continue;

            var field = Spawn(resonator.FieldProto, target);
            SetupField(field, (resonatorUid, resonator), ent.Comp.Creator ?? resonatorUid, ent.Comp.FailureProbability + ent.Comp.AddedFailure);
        }
    }

    private EntityCoordinates? SnapToTile(EntityCoordinates coords)
    {
        if (_transform.GetGrid(coords) is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return coords;

        var tile = _map.TileIndicesFor(gridUid, grid, coords);
        return _map.GridTileToLocal(gridUid, grid, tile);
    }

    private bool HasRockAt(EntityCoordinates coords)
    {
        var rocks = new HashSet<Entity<GatherableComponent>>();
        _lookup.GetEntitiesInRange(coords, 0.1f, rocks);

        foreach (var rock in rocks)
        {
            if (!TerminatingOrDeleted(rock))
                return true;
        }

        return false;
    }

    private Entity<ADTResonanceFieldComponent>? FindFieldAt(EntityCoordinates coords)
    {
        var fields = new HashSet<Entity<ADTResonanceFieldComponent>>();
        _lookup.GetEntitiesInRange(coords, 0.1f, fields);

        foreach (var field in fields)
        {
            if (!field.Comp.Bursting && !TerminatingOrDeleted(field))
                return field;
        }

        return null;
    }
}
