using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server.Power.Components;
using Content.Shared.ADT.Shields;
using Content.Shared.Emag.Systems;
using Content.Shared.Emp;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Shields;

/// <summary>Генератор щита: буфер энергии, апкип, жизненный цикл поля, режимы и UI.</summary>
public sealed partial class ShieldGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedEmpSystem _emp = default!;
    [Dependency] private readonly ShieldGridSystem _grid = default!;
    [Dependency] private readonly ShieldSegmentSystem _segment = default!;
    [Dependency] private readonly ShieldConduitSystem _conduit = default!;

    internal static readonly Direction[] RequiredDirections =
    {
        Direction.North,
        Direction.East,
        Direction.West,
    };

    private const string BuzzTwoSound = "/Audio/Machines/buzz-two.ogg";
    private const string BuzzSighSound = "/Audio/Machines/buzz-sigh.ogg";
    private const string HackSound = "/Audio/Machines/synth_explosion.ogg";

    private const double UiUpdateIntervalSeconds = 1.0;
    private const float JoulesPerMegajoule = 1000000f;

    public override void Initialize()
    {
        SubscribeLocalEvent<ShieldGeneratorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ShieldGeneratorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<ShieldGeneratorComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<ShieldGeneratorComponent, EntityTerminatingEvent>(OnTerminating);

        SubscribeLocalEvent<ShieldGeneratorComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ShieldGeneratorComponent, ShieldGeneratorToggleMessage>(OnUiToggle);
        SubscribeLocalEvent<ShieldGeneratorComponent, ShieldGeneratorEmergencyShutdownMessage>(OnUiEmergencyShutdown);
        SubscribeLocalEvent<ShieldGeneratorComponent, ShieldGeneratorSetInputCapMessage>(OnUiSetInputCap);
        SubscribeLocalEvent<ShieldGeneratorComponent, ShieldGeneratorToggleModeMessage>(OnUiToggleMode);

        SubscribeLocalEvent<ShieldGeneratorComponent, ShieldEnergyFailureEvent>(OnEnergyFailure);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ShieldGeneratorComponent>();
        while (query.MoveNext(out var uid, out var gen))
        {
            if (_timing.CurTime < gen.NextUpdate)
                continue;

            gen.NextUpdate = _timing.CurTime + TimeSpan.FromSeconds(gen.UpdatePeriod);
            ProcessGenerator(uid, gen, gen.UpdatePeriod);

            if (_timing.CurTime >= gen.NextUiUpdate)
            {
                gen.NextUiUpdate = _timing.CurTime + TimeSpan.FromSeconds(UiUpdateIntervalSeconds);
                UpdateUiState(uid, gen);
            }
        }
    }

    private void OnMapInit(EntityUid uid, ShieldGeneratorComponent gen, MapInitEvent args)
    {
        _grid.TrackGenerator(uid);
        ConfigureBattery(uid, gen);
        ScanConduits(uid, gen);
        UpdateGeneratorVisuals(uid, gen);
        _conduit.RefreshAdjacentConduits(uid, Transform(uid));

        if (gen.Running == ShieldRunningState.Running)
            _grid.GenerateField(uid, gen);
    }

    private void OnAnchorChanged(EntityUid uid, ShieldGeneratorComponent gen, ref AnchorStateChangedEvent args)
    {
        _grid.TrackGenerator(uid);

        _conduit.RefreshAdjacentConduits(uid, Transform(uid));

        if (args.Anchored)
            return;

        if (gen.RequiresAnchor && gen.Running == ShieldRunningState.Running)
        {
            gen.OfflineFor = MathF.Max(gen.OfflineFor, gen.EmergencyCooldown);
            gen.EmergencyShutdown = true;
            ShutdownMachine(uid, gen);
        }

        gen.Conduits.Clear();
        gen.LinkedDirections.Clear();
        UpdateGeneratorVisuals(uid, gen);
    }

    private void OnEmagged(EntityUid uid, ShieldGeneratorComponent gen, ref GotEmaggedEvent args)
    {
        gen.Hacked = true;
        _audio.PlayPvs(HackSound, Transform(uid).Coordinates, AudioParams.Default.WithVolume(2f));
        UpdateUiState(uid, gen);
    }

    private void OnTerminating(EntityUid uid, ShieldGeneratorComponent gen, ref EntityTerminatingEvent args)
    {
        _grid.UntrackGenerator(uid);
        _conduit.RefreshAdjacentConduits(uid, Transform(uid));
    }

    private void OnUiOpened(EntityUid uid, ShieldGeneratorComponent gen, BoundUIOpenedEvent args)
    {
        UpdateUiState(uid, gen);
    }

    private void OnUiToggle(EntityUid uid, ShieldGeneratorComponent gen, ShieldGeneratorToggleMessage msg)
    {
        if (gen.OfflineFor > 0)
        {
            _audio.PlayPvs(BuzzTwoSound, Transform(uid).Coordinates, AudioParams.Default.WithVolume(-2f));
            return;
        }

        if (gen.Running == ShieldRunningState.Off)
            StartGenerator(uid, gen);
        else if (gen.Running == ShieldRunningState.Running)
            BeginShutdown(uid, gen);

        UpdateUiState(uid, gen);
    }

    private void OnUiEmergencyShutdown(EntityUid uid, ShieldGeneratorComponent gen, ShieldGeneratorEmergencyShutdownMessage msg)
    {
        if (gen.Running == ShieldRunningState.Off)
            return;

        EmergencyShutdown(uid, gen);
        UpdateUiState(uid, gen);
    }

    private void OnUiSetInputCap(EntityUid uid, ShieldGeneratorComponent gen, ShieldGeneratorSetInputCapMessage msg)
    {
        if (gen.OfflineFor > 0)
            return;

        if (!float.IsFinite(msg.InputCap))
            return;

        gen.InputCap = Math.Clamp(msg.InputCap, 0, gen.MaxInputCap);
        if (TryComp<PowerNetworkBatteryComponent>(uid, out var netBattery))
            netBattery.MaxChargeRate = gen.InputCap;

        UpdateUiState(uid, gen);
    }

    private void OnUiToggleMode(EntityUid uid, ShieldGeneratorComponent gen, ShieldGeneratorToggleModeMessage msg)
    {
        if (gen.OfflineFor > 0)
            return;

        ToggleMode(uid, gen, msg.Mode);
        UpdateUiState(uid, gen);
    }

    private void OnEnergyFailure(EntityUid uid, ShieldGeneratorComponent gen, ref ShieldEnergyFailureEvent args)
    {
        if (gen.Running == ShieldRunningState.Discharging)
        {
            ShutdownMachine(uid, gen);
        }
        else
        {
            if (TryComp<BatteryComponent>(uid, out var battery))
                _battery.SetCharge((uid, battery), 0);

            OverloadField(uid, gen);
        }
    }

    private void ProcessGenerator(EntityUid uid, ShieldGeneratorComponent gen, float dt)
    {
        if (TerminatingOrDeleted(uid))
            return;

        if (gen.OfflineFor > 0)
        {
            gen.OfflineFor = MathF.Max(0, gen.OfflineFor - dt);
            if (gen.OfflineFor <= 0)
                gen.EmergencyShutdown = false;
            return;
        }

        if (gen.Running == ShieldRunningState.Discharging)
        {
            gen.DischargeTimer += dt;

            if (TryComp<BatteryComponent>(uid, out var battery))
                _battery.ChangeCharge((uid, battery), -ShieldConstants.ShutdownDispersionRate * dt);

            if (gen.DischargeTimer >= gen.DischargeDuration)
            {
                ShutdownMachine(uid, gen);
                gen.OfflineFor = MathF.Max(gen.OfflineFor, gen.GracefulCooldown);
            }
            return;
        }

        if (gen.Running != ShieldRunningState.Running)
            return;

        if ((gen.RequiresAnchor && !Transform(uid).Anchored)
            || (gen.RequiresConduits && GetLinkedConduitCount(gen) < ShieldGeneratorComponent.RequiredConduitCount))
        {
            EmergencyShutdown(uid, gen);
            return;
        }

        DecayMitigation(gen, dt);

        if (TryComp<BatteryComponent>(uid, out var runningBattery))
        {
            var activeSegments = gen.Tiles.Count - gen.DamagedTiles.Count;
            var upkeep = activeSegments * ShieldConstants.EnergyUpkeepPerTile * gen.UpkeepMultiplier * dt;
            gen.CurrentUpkeep = upkeep;
            _battery.ChangeCharge((uid, runningBattery), -upkeep);

            if (_battery.GetCharge((uid, runningBattery)) <= 0)
                OverloadField(uid, gen);
        }

        if (gen.Overloaded <= 0)
        {
            if (Transform(uid).GridUid is { } gridUid)
            {
                foreach (var tile in gen.DamagedTiles.ToList())
                {
                    var segUid = _grid.GetSegmentAt(gridUid, tile);
                    if (!Exists(segUid) || !TryComp<ShieldSegmentComponent>(segUid, out var segComp))
                        continue;

                    _segment.Regenerate(segUid, segComp, gen, dt);
                }
            }
        }
        else if (_segment.FieldIntegrity(uid, gen) > gen.OverloadClearIntegrity)
        {
            gen.Overloaded = 0;
        }

        UpdateGeneratorVisuals(uid, gen);
    }

    public void StartGenerator(EntityUid uid, ShieldGeneratorComponent gen)
    {
        if (gen.RequiresAnchor && !Transform(uid).Anchored)
        {
            _audio.PlayPvs(BuzzSighSound, Transform(uid).Coordinates);
            return;
        }

        ScanConduits(uid, gen);
        if (gen.RequiresConduits && GetLinkedConduitCount(gen) < ShieldGeneratorComponent.RequiredConduitCount)
        {
            _audio.PlayPvs(BuzzTwoSound, Transform(uid).Coordinates);
            return;
        }

        gen.Running = ShieldRunningState.Running;
        gen.DischargeTimer = 0;
        gen.Overloaded = 0;
        _grid.GenerateField(uid, gen);
        gen.OfflineFor = gen.StartCooldown;
        UpdateGeneratorVisuals(uid, gen);
    }

    public void BeginShutdown(EntityUid uid, ShieldGeneratorComponent gen)
    {
        gen.Running = ShieldRunningState.Discharging;
        gen.DischargeTimer = 0;
        UpdateGeneratorVisuals(uid, gen);
    }

    public void EmergencyShutdown(EntityUid uid, ShieldGeneratorComponent gen)
    {
        if (gen.Running == ShieldRunningState.Off)
            return;

        var integrity = _segment.FieldIntegrity(uid, gen);

        ShutdownMachine(uid, gen);
        gen.OfflineFor = MathF.Max(gen.OfflineFor, gen.EmergencyCooldown);
        gen.EmergencyShutdown = true;

        if (_random.Next(100) < (integrity - gen.EmpChanceThreshold) * gen.EmpChanceScale)
            _emp.EmpPulse(Transform(uid).Coordinates, gen.EmpPulseRadius, gen.EmpPulseEnergy, TimeSpan.FromSeconds(gen.EmpPulseDuration), uid);
    }

    public void ShutdownMachine(EntityUid uid, ShieldGeneratorComponent gen)
    {
        _grid.RemoveAllSegments(uid, gen);
        gen.Running = ShieldRunningState.Off;
        gen.MitigationPhysical = 0;
        gen.MitigationEm = 0;
        gen.MitigationHeat = 0;
        gen.CurrentUpkeep = 0;
        UpdateGeneratorVisuals(uid, gen);
    }

    private void OverloadField(EntityUid uid, ShieldGeneratorComponent gen)
    {
        gen.Overloaded = 1;
        foreach (var seg in gen.Segments)
        {
            if (TryComp<ShieldSegmentComponent>(seg, out var segComp))
                _segment.Fail(seg, segComp, 1f);
        }
    }

    private static void DecayMitigation(ShieldGeneratorComponent gen, float dt)
    {
        gen.MitigationPhysical = MathF.Max(0, gen.MitigationPhysical - ShieldConstants.MitigationLossPassive * dt);
        gen.MitigationEm = MathF.Max(0, gen.MitigationEm - ShieldConstants.MitigationLossPassive * dt);
        gen.MitigationHeat = MathF.Max(0, gen.MitigationHeat - ShieldConstants.MitigationLossPassive * dt);
    }

    private void ToggleMode(EntityUid uid, ShieldGeneratorComponent gen, ShieldModes mode)
    {
        var info = ShieldModesHelpers.AllModes.FirstOrDefault(m => m.Flag == mode);

        if (mode == ShieldModes.None || info.Flag != mode)
            return;

        if (info.HackedOnly && !gen.Hacked)
        {
            _audio.PlayPvs(BuzzTwoSound, Transform(uid).Coordinates, AudioParams.Default.WithVolume(-2f));
            return;
        }

        gen.Modes ^= mode;

        if (gen.Modes.HasMode(ShieldModes.Modulate))
            gen.MitigationPhysical = gen.MitigationEm = gen.MitigationHeat = 0;

        gen.UpkeepMultiplier = gen.Modes.GetUpkeepMultiplier();

        foreach (var seg in gen.Segments)
        {
            if (TryComp<ShieldSegmentComponent>(seg, out var segComp))
                _segment.UpdateSegmentVisuals(seg, segComp, gen);
        }

        UpdateGeneratorVisuals(uid, gen);
    }

    public void ScanConduits(EntityUid uid, ShieldGeneratorComponent gen)
    {
        gen.Conduits.Clear();
        gen.LinkedDirections.Clear();

        if (!gen.RequiresConduits)
        {
            FinishScan(uid, gen);
            return;
        }

        if (!Transform(uid).Anchored || Transform(uid).GridUid is not { } gridUid)
        {
            FinishScan(uid, gen);
            return;
        }

        if (!TryComp<MapGridComponent>(gridUid, out var gridComp))
            return;

        var genTile = _map.TileIndicesFor(gridUid, gridComp, Transform(uid).Coordinates);

        foreach (var dir in RequiredDirections)
        {
            var tile = genTile + dir.ToIntVec();
            var enumerator = _map.GetAnchoredEntitiesEnumerator(gridUid, gridComp, tile);
            while (enumerator.MoveNext(out var ent))
            {
                if (!TryComp<ShieldConduitComponent>(ent.Value, out var conduit))
                    continue;

                conduit.Generator = uid;
                gen.Conduits.Add(ent.Value);
                gen.LinkedDirections.Add(dir);
                break;
            }
        }

        FinishScan(uid, gen);
    }

    private void FinishScan(EntityUid uid, ShieldGeneratorComponent gen)
    {
        ConfigureBattery(uid, gen);
        UpdateGeneratorVisuals(uid, gen);
    }

    private void ConfigureBattery(EntityUid uid, ShieldGeneratorComponent gen)
    {
        var conduitCount = gen.RequiresConduits
            ? GetLinkedConduitCount(gen)
            : ShieldGeneratorComponent.RequiredConduitCount;

        if (TryComp<BatteryComponent>(uid, out var battery))
            _battery.SetMaxCharge((uid, battery), gen.MaxChargeBase + conduitCount * gen.ChargePerConduit);

        gen.MaxInputCap = conduitCount * gen.InputPerConduit;
        gen.InputCap = Math.Clamp(gen.InputCap, 0, gen.MaxInputCap);

        if (TryComp<PowerNetworkBatteryComponent>(uid, out var netBattery))
            netBattery.MaxChargeRate = gen.InputCap;
    }

    private int GetLinkedConduitCount(ShieldGeneratorComponent gen)
    {
        var count = 0;
        foreach (var conduit in gen.Conduits)
        {
            if (Exists(conduit)
                && !TerminatingOrDeleted(conduit)
                && TryComp<ShieldConduitComponent>(conduit, out var comp)
                && comp.Generator == gen.Owner)
            {
                count++;
            }
        }
        return count;
    }

    private void UpdateUiState(EntityUid uid, ShieldGeneratorComponent gen)
    {
        if (!_ui.HasUi(uid, ShieldGeneratorUiKey.Key))
            return;

        var currentEnergy = 0f;
        var maxEnergy = 0f;
        if (TryComp<BatteryComponent>(uid, out var battery))
        {
            currentEnergy = _battery.GetCharge((uid, battery));
            maxEnergy = battery.MaxCharge;
        }

        var state = new ShieldGeneratorBuiState
        {
            Running = gen.Running,
            Overloaded = gen.Overloaded > 0,
            FieldIntegrity = _segment.FieldIntegrity(uid, gen),
            CurrentEnergy = currentEnergy / JoulesPerMegajoule,
            MaxEnergy = maxEnergy / JoulesPerMegajoule,
            InputCap = gen.InputCap,
            MaxInputCap = gen.MaxInputCap,
            CurrentUpkeep = gen.CurrentUpkeep,
            TotalSegments = gen.Tiles.Count,
            FunctionalSegments = gen.Tiles.Count - gen.DamagedTiles.Count,
            OfflineFor = gen.OfflineFor,
            EmergencyShutdown = gen.EmergencyShutdown,
            ConduitsDeployed = gen.RequiresConduits ? GetLinkedConduitCount(gen) : ShieldGeneratorComponent.RequiredConduitCount,
            RequiredConduits = ShieldGeneratorComponent.RequiredConduitCount,
            Hacked = gen.Hacked,
            Modes = BuildModeEntries(gen),
        };

        if (MatchesLastState(gen, state))
            return;

        CacheLastState(gen, state);

        _ui.SetUiState(uid, ShieldGeneratorUiKey.Key, state);
    }

    private static bool MatchesLastState(ShieldGeneratorComponent gen, ShieldGeneratorBuiState state)
        => gen.LastUiRunning == state.Running
           && gen.LastUiOverloaded == state.Overloaded
           && gen.LastUiIntegrity == state.FieldIntegrity
           && gen.LastUiEnergy == state.CurrentEnergy
           && gen.LastUiUpkeep == state.CurrentUpkeep
           && gen.LastUiOffline == state.OfflineFor
           && gen.LastUiTotalSegments == state.TotalSegments
           && gen.LastUiFunctionalSegments == state.FunctionalSegments
           && gen.LastUiConduits == state.ConduitsDeployed
           && gen.LastUiModes == (int) gen.Modes;

    private static void CacheLastState(ShieldGeneratorComponent gen, ShieldGeneratorBuiState state)
    {
        gen.LastUiRunning = state.Running;
        gen.LastUiOverloaded = state.Overloaded;
        gen.LastUiIntegrity = state.FieldIntegrity;
        gen.LastUiEnergy = state.CurrentEnergy;
        gen.LastUiUpkeep = state.CurrentUpkeep;
        gen.LastUiOffline = state.OfflineFor;
        gen.LastUiTotalSegments = state.TotalSegments;
        gen.LastUiFunctionalSegments = state.FunctionalSegments;
        gen.LastUiConduits = state.ConduitsDeployed;
        gen.LastUiModes = (int) gen.Modes;
    }

    private List<ShieldGeneratorModeEntry> BuildModeEntries(ShieldGeneratorComponent gen)
    {
        var list = new List<ShieldGeneratorModeEntry>();
        foreach (var mode in ShieldModesHelpers.AllModes)
        {
            list.Add(new ShieldGeneratorModeEntry
            {
                Flag = mode.Flag,
                Name = mode.Name,
                Description = mode.Description,
                Multiplier = mode.Multiplier,
                Enabled = gen.Modes.HasMode(mode.Flag),
                HackedOnly = mode.HackedOnly,
                Hackable = gen.Hacked,
            });
        }
        return list;
    }

    private void UpdateGeneratorVisuals(EntityUid uid, ShieldGeneratorComponent gen)
    {
        var running = gen.Running == ShieldRunningState.Running;

        if (TryComp<AppearanceComponent>(uid, out _))
        {
            _appearance.SetData(uid, ShieldGeneratorVisuals.Running, running);
            _appearance.SetData(uid, ShieldGeneratorVisuals.CapacitorNorth, gen.LinkedDirections.Contains(Direction.North));
            _appearance.SetData(uid, ShieldGeneratorVisuals.CapacitorEast, gen.LinkedDirections.Contains(Direction.East));
            _appearance.SetData(uid, ShieldGeneratorVisuals.CapacitorWest, gen.LinkedDirections.Contains(Direction.West));
        }
    }
}