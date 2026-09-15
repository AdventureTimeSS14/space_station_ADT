using Content.Server.ADT.LogicCircuit.Components;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.LogicCircuit;
using Content.Shared.ADT.LogicCircuit.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Content.Server.ADT.LogicCircuit;

public sealed partial class ADTLogicCircuitSystem : SharedADTLogicCircuitSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private bool _logicEnabled = true;
    private int _tickInterval = 2;
    private int _maxActiveCircuits = 64;
    private int _maxSignalLength = LogicSignal.DefaultMaxLength;
    private float _budgetMs = 2f;

    private readonly List<Entity<ADTLogicCircuitComponent>> _pending = new();
    private readonly Dictionary<EntityUid, Dictionary<string, LogicSignal>> _wireless = new();

    private int _cursor;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(Cfg, ADTCCVars.LogicEnabled, value => _logicEnabled = value, true);
        Subs.CVar(Cfg, ADTCCVars.LogicTickInterval, value => _tickInterval = Math.Max(1, value), true);
        Subs.CVar(Cfg, ADTCCVars.LogicMaxActiveCircuits, value => _maxActiveCircuits = Math.Max(1, value), true);
        Subs.CVar(Cfg, ADTCCVars.LogicMaxSignalLength, value => _maxSignalLength = Math.Max(1, value), true);
        Subs.CVar(Cfg, ADTCCVars.LogicBudgetMs, value => _budgetMs = MathF.Max(0.1f, value), true);

        SubscribeLocalEvent<ADTLogicCircuitComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTLogicCircuitComponent, ComponentShutdown>(OnShutdown);

        //SubscribeLocalEvent<MapComponent, ComponentShutdown>(OnMapShutdown); todo fix

        InitializeBridge();
        InitializeUi();
        InitializeDisk();
    }

    private void OnMapInit(Entity<ADTLogicCircuitComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;

        comp.LastTick = _timing.CurTime;

        Normalize(comp.Layout);

        if (!TryValidate(comp.Layout, GetLimits(comp), out var error, out var detail))
        {
            comp.Broken = true;
            Log.Error(
                $"Схема кабельной коробки {ToPrettyString(ent)} не прошла проверку: {error} ({detail})");
            return;
        }

        comp.Compiled = CompiledLogicCircuit.Compile(comp.Layout, Prototypes);
        comp.Broken = false;

        Wake(ent);
    }

    private void OnMapShutdown(Entity<MapComponent> ent, ref ComponentShutdown args)
    {
        _wireless.Remove(ent.Owner);
    }

    private void OnShutdown(Entity<ADTLogicCircuitComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.Compiled = null;
        RemCompDeferred<ADTActiveLogicCircuitComponent>(ent);
    }

    public void Wake(Entity<ADTLogicCircuitComponent> ent)
    {
        var comp = ent.Comp;

        if (comp.Broken || !comp.Enabled || comp.Compiled == null)
            return;

        comp.IdleTicks = 0;
        EnsureComp<ADTActiveLogicCircuitComponent>(ent);
    }

    public void SetPortInput(Entity<ADTLogicCircuitComponent> ent, int port, LogicSignal value)
    {
        SetPortInputByIndex(ent, port - 1, value, false);
    }

    private void SetPortInputByIndex(
        Entity<ADTLogicCircuitComponent> ent,
        int index,
        LogicSignal value,
        bool momentary)
    {
        var comp = ent.Comp;

        if ((uint) index >= (uint) comp.PortInputs.Length)
            return;

        if (momentary && index < 64)
            comp.MomentaryPorts |= 1UL << index;

        if (comp.PortInputs[index] == value)
            return;

        comp.PortInputs[index] = value;
        Wake(ent);
    }

    private static void ClearMomentaryPorts(ADTLogicCircuitComponent comp)
    {
        var mask = comp.MomentaryPorts;
        comp.MomentaryPorts = 0;

        for (var i = 0; mask != 0 && i < comp.PortInputs.Length; i++)
        {
            var bit = 1UL << i;

            if ((mask & bit) == 0)
                continue;

            mask &= ~bit;
            comp.PortInputs[i] = LogicSignal.Empty;
        }
    }

    public bool TryApplyLayout(
        Entity<ADTLogicCircuitComponent> ent,
        LogicCircuitLayout layout,
        out LogicCircuitValidationError error,
        out string detail)
    {
        var comp = ent.Comp;
        var candidate = layout.Clone();

        Normalize(candidate);

        if (!TryValidate(candidate, GetLimits(comp), out error, out detail))
            return false;

        comp.Layout = candidate;
        comp.Compiled = CompiledLogicCircuit.Compile(candidate, Prototypes);
        comp.Broken = false;
        comp.LastTick = _timing.CurTime;

        Wake(ent);
        return true;
    }

    public void SetEnabled(Entity<ADTLogicCircuitComponent> ent, bool enabled)
    {
        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;

        if (enabled)
        {
            ent.Comp.LastTick = _timing.CurTime;
            Wake(ent);
            return;
        }

        RemComp<ADTActiveLogicCircuitComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        if (!_logicEnabled)
            return;

        if (_timing.CurTick.Value % (uint) _tickInterval != 0)
            return;

        _pending.Clear();

        var query = EntityQueryEnumerator<ADTActiveLogicCircuitComponent, ADTLogicCircuitComponent>();
        while (query.MoveNext(out var uid, out _, out var circuit))
        {
            _pending.Add((uid, circuit));
        }

        if (_pending.Count == 0)
            return;

        var budgetTicks = (long) (_budgetMs / 1000d * Stopwatch.Frequency);
        var start = Stopwatch.GetTimestamp();
        var allowed = Math.Min(_pending.Count, _maxActiveCircuits);

        if (_cursor >= _pending.Count)
            _cursor = 0;

        var processed = 0;
        while (processed < allowed)
        {
            var ent = _pending[_cursor];
            _cursor = (_cursor + 1) % _pending.Count;
            processed++;

            TickCircuit(ent);

            if (Stopwatch.GetTimestamp() - start >= budgetTicks)
                break;
        }
    }

    private Dictionary<string, LogicSignal>? GetWirelessChannels(EntityUid uid)
    {
        var map = Transform(uid).MapUid;

        if (map == null)
            return null;

        if (_wireless.TryGetValue(map.Value, out var channels))
            return channels;

        channels = new Dictionary<string, LogicSignal>();
        _wireless[map.Value] = channels;
        return channels;
    }

    private void TickCircuit(Entity<ADTLogicCircuitComponent> ent)
    {
        var comp = ent.Comp;
        var compiled = comp.Compiled;

        if (compiled == null || !comp.Enabled)
        {
            RemComp<ADTActiveLogicCircuitComponent>(ent);
            return;
        }

        var curTime = _timing.CurTime;
        var delta = (float) (curTime - comp.LastTick).TotalSeconds;
        comp.LastTick = curTime;

        var changed = compiled.Tick(
            comp.PortInputs,
            comp.PortOutputs,
            delta,
            curTime,
            _maxSignalLength,
            GetWirelessChannels(ent),
            out var keepAwake);

        FlushPortOutputs(ent);
        PushLiveValues(ent);

        var momentary = comp.MomentaryPorts != 0;

        if (momentary)
            ClearMomentaryPorts(comp);

        if (changed || keepAwake || momentary)
        {
            comp.IdleTicks = 0;
            return;
        }

        comp.IdleTicks++;

        if (comp.IdleTicks >= IdleTicksBeforeSleep)
            RemComp<ADTActiveLogicCircuitComponent>(ent);
    }

    partial void InitializeBridge();
    partial void FlushPortOutputs(Entity<ADTLogicCircuitComponent> ent);
    partial void InitializeUi();
    partial void PushLiveValues(Entity<ADTLogicCircuitComponent> ent);
    partial void InitializeDisk();
}
