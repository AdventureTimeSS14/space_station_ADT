using Content.Shared.ADT.InconnuOS;
using Content.Shared.ADT.LogicCircuit;
using Content.Shared.ADT.LogicCircuit.Components;
using Content.Shared.Popups;

namespace Content.Server.ADT.LogicCircuit;

public sealed partial class ADTLogicCircuitSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly TimeSpan ValueInterval = TimeSpan.FromMilliseconds(200);

    private static readonly Enum[] UiKeys =
    {
        ADTLogicCircuitUiKey.Key,
        ADTComputerUiKey.Key,
    };

    private static bool IsEditorKey(Enum key)
    {
        return key is ADTLogicCircuitUiKey or ADTComputerUiKey;
    }

    partial void InitializeUi()
    {
        SubscribeLocalEvent<ADTLogicCircuitComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ADTLogicCircuitComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<ADTLogicCircuitComponent, ADTLogicCircuitApplyMessage>(OnApply);
        SubscribeLocalEvent<ADTLogicCircuitComponent, ADTLogicCircuitSetEnabledMessage>(OnSetEnabled);
        SubscribeLocalEvent<ADTLogicCircuitComponent, ADTOsRequestCircuitMessage>(OnRequestState);
    }

    private void OnUiOpened(EntityUid uid, ADTLogicCircuitComponent comp, BoundUIOpenedEvent args)
    {
        if (!IsEditorKey(args.UiKey))
            return;

        var ent = new Entity<ADTLogicCircuitComponent>(uid, comp);
        ent.Comp.UiOpen = true;
        ent.Comp.NextValueSend = TimeSpan.Zero;

        UpdateUiState(ent);
        SendValues(ent);
    }

    private void OnUiClosed(EntityUid uid, ADTLogicCircuitComponent comp, BoundUIClosedEvent args)
    {
        if (!IsEditorKey(args.UiKey))
            return;

        var ent = new Entity<ADTLogicCircuitComponent>(uid, comp);

        ent.Comp.UiOpen = _ui.IsUiOpen(ent.Owner, UiKeys);
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

    private void OnRequestState(EntityUid uid, ADTLogicCircuitComponent comp, ADTOsRequestCircuitMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        var ent = new Entity<ADTLogicCircuitComponent>(uid, comp);
        var state = BuildState(ent);

        _ui.ServerSendUiMessage(uid, ADTComputerUiKey.Key, new ADTOsCircuitStateMessage(state), actor);

        SendPorts(ent, actor);
    }

    private void SendPorts(Entity<ADTLogicCircuitComponent> ent, EntityUid? actor = null)
    {
        var comp = ent.Comp;

        var inputs = new OsPortInfo[comp.InputPorts.Count];
        var outputs = new OsPortInfo[comp.OutputPorts.Count];

        for (var i = 0; i < inputs.Length; i++)
        {
            inputs[i] = new OsPortInfo
            {
                Port = comp.InputPorts[i].Id,
                Value = i < comp.PortInputs.Length ? comp.PortInputs[i] : LogicSignal.Empty,
                Links = -1,
            };
        }

        for (var i = 0; i < outputs.Length; i++)
        {
            outputs[i] = new OsPortInfo
            {
                Port = comp.OutputPorts[i].Id,
                Value = i < comp.PortOutputs.Length ? comp.PortOutputs[i] : LogicSignal.Empty,
                Links = _deviceLink.GetLinkedSinks(ent.Owner, comp.OutputPorts[i]).Count,
            };
        }

        var message = new ADTOsPortsMessage(inputs, outputs);

        if (actor is { } target)
        {
            _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key, message, target);
            return;
        }

        _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key, message);
    }

    private ADTLogicCircuitBuiState BuildState(Entity<ADTLogicCircuitComponent> ent)
    {
        var comp = ent.Comp;

        return new ADTLogicCircuitBuiState(
            comp.Layout,
            GetLimits(comp),
            GetPowerUsed(comp.Layout),
            comp.InputPorts.Count,
            comp.OutputPorts.Count,
            comp.Enabled,
            comp.Broken);
    }

    private void UpdateUiState(Entity<ADTLogicCircuitComponent> ent)
    {
        var state = BuildState(ent);

        if (_ui.HasUi(ent.Owner, ADTLogicCircuitUiKey.Key))
            _ui.SetUiState(ent.Owner, ADTLogicCircuitUiKey.Key, state);

        if (_ui.IsUiOpen(ent.Owner, ADTComputerUiKey.Key))
            _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key, new ADTOsCircuitStateMessage(state));
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

        comp.NextValueSend = _timing.CurTime + ValueInterval;

        if (_ui.IsUiOpen(ent.Owner, ADTComputerUiKey.Key))
            SendPorts(ent);

        var compiled = comp.Compiled;

        if (compiled == null)
            return;

        var values = new LogicSignal[compiled.Prev.Length];
        Array.Copy(compiled.Prev, values, values.Length);

        foreach (var key in UiKeys)
        {
            if (_ui.IsUiOpen(ent.Owner, key))
                _ui.ServerSendUiMessage(ent.Owner, key, new ADTLogicCircuitValuesMessage(values));
        }
    }
}
