using Content.Server.DeviceLinking.Systems;
using Content.Shared.ADT.LogicCircuit;
using Content.Shared.ADT.LogicCircuit.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;

namespace Content.Server.ADT.LogicCircuit;

public sealed partial class ADTLogicCircuitSystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;

    public const string LogicValueKey = "adt_logic_value";

    partial void InitializeBridge()
    {
        SubscribeLocalEvent<ADTLogicCircuitComponent, ComponentInit>(OnBridgeInit);
        SubscribeLocalEvent<ADTLogicCircuitComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ADTLogicCircuitComponent, NewLinkEvent>(OnNewLink);
    }

    private void OnBridgeInit(Entity<ADTLogicCircuitComponent> ent, ref ComponentInit args)
    {
        var comp = ent.Comp;

        comp.PortInputs = new LogicSignal[comp.InputPorts.Count];
        comp.PortOutputs = new LogicSignal[comp.OutputPorts.Count];
        comp.SentPortOutputs = new LogicSignal[comp.OutputPorts.Count];

        if (comp.InputPorts.Count > 0)
            _deviceLink.EnsureSinkPorts(ent, comp.InputPorts.ToArray());

        if (comp.OutputPorts.Count > 0)
            _deviceLink.EnsureSourcePorts(ent, comp.OutputPorts.ToArray());
    }

    private void OnSignalReceived(Entity<ADTLogicCircuitComponent> ent, ref SignalReceivedEvent args)
    {
        var received = args.Port;
        var index = ent.Comp.InputPorts.FindIndex(port => port.Id == received);

        if (index < 0)
            return;

        if (args.Data != null
            && args.Data.TryGetValue(LogicValueKey, out string? text)
            && text != null)
        {
            SetPortInputByIndex(ent, index, LogicSignal.FromText(text, _maxSignalLength), false);
            return;
        }

        var state = SignalState.Momentary;
        args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

        switch (state)
        {
            case SignalState.High:
                SetPortInputByIndex(ent, index, LogicSignal.True, false);
                break;
            case SignalState.Low:
                SetPortInputByIndex(ent, index, LogicSignal.False, false);
                break;
            default:
                SetPortInputByIndex(ent, index, LogicSignal.True, true);
                break;
        }
    }

    private void OnNewLink(Entity<ADTLogicCircuitComponent> ent, ref NewLinkEvent args)
    {
        if (args.Source != ent.Owner)
            return;

        var sourcePort = args.SourcePort;
        var index = ent.Comp.OutputPorts.FindIndex(port => port.Id == sourcePort);

        if (index < 0)
            return;

        if (index >= ent.Comp.SentPortOutputs.Length)
            return;

        SendPort(ent, index, ent.Comp.SentPortOutputs[index]);
    }

    partial void FlushPortOutputs(Entity<ADTLogicCircuitComponent> ent)
    {
        var comp = ent.Comp;

        for (var i = 0; i < comp.PortOutputs.Length; i++)
        {
            if (comp.PortOutputs[i] == comp.SentPortOutputs[i])
                continue;

            comp.SentPortOutputs[i] = comp.PortOutputs[i];
            SendPort(ent, i, comp.PortOutputs[i]);
        }
    }

    private void SendPort(Entity<ADTLogicCircuitComponent> ent, int index, LogicSignal value)
    {
        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.LogicState] = value.AsBool() ? SignalState.High : SignalState.Low,
            [LogicValueKey] = value.AsText(),
        };

        _deviceLink.InvokePort(ent, ent.Comp.OutputPorts[index], payload);
    }
}
