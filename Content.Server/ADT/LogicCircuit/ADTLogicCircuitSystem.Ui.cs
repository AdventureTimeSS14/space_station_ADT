using Content.Shared.ADT.LogicCircuit;
using Content.Shared.ADT.LogicCircuit.Components;
using Content.Shared.Popups;

namespace Content.Server.ADT.LogicCircuit;

public sealed partial class ADTLogicCircuitSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly TimeSpan ValueInterval = TimeSpan.FromMilliseconds(200);

    partial void InitializeUi()
    {
        SubscribeLocalEvent<ADTLogicCircuitComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ADTLogicCircuitComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<ADTLogicCircuitComponent, ADTLogicCircuitApplyMessage>(OnApply);
        SubscribeLocalEvent<ADTLogicCircuitComponent, ADTLogicCircuitSetEnabledMessage>(OnSetEnabled);
    }

    private void OnUiOpened(EntityUid uid, ADTLogicCircuitComponent comp, BoundUIOpenedEvent args)
    {
        if (args.UiKey is not ADTLogicCircuitUiKey)
            return;

        var ent = new Entity<ADTLogicCircuitComponent>(uid, comp);
        ent.Comp.UiOpen = true;
        ent.Comp.NextValueSend = TimeSpan.Zero;

        UpdateUiState(ent);
        SendValues(ent);
    }

    private void OnUiClosed(EntityUid uid, ADTLogicCircuitComponent comp, BoundUIClosedEvent args)
    {
        if (args.UiKey is not ADTLogicCircuitUiKey)
            return;

        var ent = new Entity<ADTLogicCircuitComponent>(uid, comp);

        ent.Comp.UiOpen = _ui.IsUiOpen(ent.Owner, ADTLogicCircuitUiKey.Key);
    }

    private void OnApply(EntityUid uid, ADTLogicCircuitComponent comp, ADTLogicCircuitApplyMessage args)
    {
        var ent = new Entity<ADTLogicCircuitComponent>(uid, comp);

        if (TryApplyLayout(ent, args.Layout, out var error, out var detail))
        {
            UpdateUiState(ent);
            return;
        }

        if (args.Actor is { Valid: true } actor)
            _popup.PopupEntity(LogicCircuitErrors.GetMessage(error, detail), ent, actor);

        UpdateUiState(ent);
    }

    private void OnSetEnabled(EntityUid uid, ADTLogicCircuitComponent comp, ADTLogicCircuitSetEnabledMessage args)
    {
        var ent = new Entity<ADTLogicCircuitComponent>(uid, comp);

        SetEnabled(ent, args.Enabled);
        UpdateUiState(ent);
    }

    private void UpdateUiState(Entity<ADTLogicCircuitComponent> ent)
    {
        var comp = ent.Comp;

        var state = new ADTLogicCircuitBuiState(
            comp.Layout,
            GetLimits(comp),
            GetPowerUsed(comp.Layout),
            comp.InputPorts.Count,
            comp.OutputPorts.Count,
            comp.Enabled,
            comp.Broken);

        _ui.SetUiState(ent.Owner, ADTLogicCircuitUiKey.Key, state);
    }

    partial void PushLiveValues(Entity<ADTLogicCircuitComponent> ent)
    {
        var comp = ent.Comp;

        if (!comp.UiOpen)
            return;

        var now = _timing.CurTime;

        if (now < comp.NextValueSend)
            return;

        SendValues(ent);
    }

    private void SendValues(Entity<ADTLogicCircuitComponent> ent)
    {
        var comp = ent.Comp;
        var compiled = comp.Compiled;

        if (compiled == null)
            return;

        comp.NextValueSend = _timing.CurTime + ValueInterval;

        var values = new LogicSignal[compiled.Prev.Length];
        Array.Copy(compiled.Prev, values, values.Length);

        _ui.ServerSendUiMessage(ent.Owner, ADTLogicCircuitUiKey.Key, new ADTLogicCircuitValuesMessage(values));
    }
}
