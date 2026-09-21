using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.LogicCircuit.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.LogicCircuit;

public abstract class SharedADTLogicCircuitSystem : EntitySystem
{
    [Dependency] protected readonly IConfigurationManager Cfg = default!;
    [Dependency] protected readonly IPrototypeManager Prototypes = default!;

    public const int MaxNodeIdLength = 32;
    public const int IdleTicksBeforeSleep = 4;

    public LogicCircuitLimits GetLimits(ADTLogicCircuitComponent component)
    {
        return new LogicCircuitLimits
        {
            MaxNodes = Cfg.GetCVar(ADTCCVars.LogicMaxElements),
            MaxWires = Cfg.GetCVar(ADTCCVars.LogicMaxWires),
            MaxSignalLength = Cfg.GetCVar(ADTCCVars.LogicMaxSignalLength),
            MaxConfigLength = Cfg.GetCVar(ADTCCVars.LogicMaxConfigLength),
            PowerBudget = component.PowerBudget,
        };
    }

    public int GetPowerUsed(LogicCircuitLayout layout)
    {
        var total = 0;

        foreach (var node in layout.Nodes)
        {
            if (Prototypes.TryIndex(node.Proto, out var proto))
                total += proto.Cost;
        }

        return total;
    }

    public void Normalize(LogicCircuitLayout layout)
    {
        foreach (var node in layout.Nodes)
        {
            if (!Prototypes.TryIndex(node.Proto, out var proto))
                continue;

            NormalizeConfig(node, proto);
            NormalizeState(node, proto);
        }
    }

    public bool IsNormalized(LogicCircuitLayout layout)
    {
        foreach (var node in layout.Nodes)
        {
            if (!Prototypes.TryIndex(node.Proto, out var proto))
                continue;

            if (node.Config.Count != proto.Config.Count || node.State.Length != proto.StateSize)
                return false;

            for (var i = 0; i < node.Config.Count; i++)
            {
                var field = proto.Config[i];
                if (field.Type == LogicConfigFieldType.Text)
                    continue;

                var value = node.Config[i];
                if (!value.IsNumber || Math.Clamp(value.Number, field.Min, field.Max) != value.Number)
                    return false;
            }
        }

        return true;
    }

    private static void NormalizeConfig(LogicNodeData node, LogicElementPrototype proto)
    {
        while (node.Config.Count > proto.Config.Count)
        {
            node.Config.RemoveAt(node.Config.Count - 1);
        }

        while (node.Config.Count < proto.Config.Count)
        {
            var field = proto.Config[node.Config.Count];
            node.Config.Add(LogicSignal.FromText(field.Default));
        }

        for (var i = 0; i < node.Config.Count; i++)
        {
            var field = proto.Config[i];
            if (field.Type == LogicConfigFieldType.Text)
                continue;

            var value = node.Config[i];
            if (!value.IsNumber)
            {
                node.Config[i] = LogicSignal.FromText(field.Default);
                continue;
            }

            var clamped = Math.Clamp(value.Number, field.Min, field.Max);
            if (clamped != value.Number)
                node.Config[i] = LogicSignal.FromNumber(clamped);
        }
    }

    private static void NormalizeState(LogicNodeData node, LogicElementPrototype proto)
    {
        if (node.State.Length == proto.StateSize)
            return;

        var state = new LogicSignal[proto.StateSize];
        var shared = Math.Min(state.Length, node.State.Length);
        for (var i = 0; i < shared; i++)
        {
            state[i] = node.State[i];
        }

        node.State = state;
    }

    public bool TryValidate(
        LogicCircuitLayout layout,
        LogicCircuitLimits limits,
        out LogicCircuitValidationError error,
        out string detail)
    {
        error = LogicCircuitValidationError.None;
        detail = string.Empty;

        if (layout.Nodes.Count > limits.MaxNodes)
        {
            error = LogicCircuitValidationError.TooManyNodes;
            detail = limits.MaxNodes.ToString();
            return false;
        }

        if (layout.Wires.Count > limits.MaxWires)
        {
            error = LogicCircuitValidationError.TooManyWires;
            detail = limits.MaxWires.ToString();
            return false;
        }

        var nodes = new Dictionary<string, LogicElementPrototype>(layout.Nodes.Count);
        var power = 0;

        foreach (var node in layout.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id))
            {
                error = LogicCircuitValidationError.EmptyNodeId;
                return false;
            }

            if (node.Id.Length > MaxNodeIdLength)
            {
                error = LogicCircuitValidationError.NodeIdTooLong;
                detail = node.Id[..MaxNodeIdLength];
                return false;
            }

            if (!Prototypes.TryIndex(node.Proto, out var proto))
            {
                error = LogicCircuitValidationError.UnknownElement;
                detail = node.Proto.Id;
                return false;
            }

            if (!nodes.TryAdd(node.Id, proto))
            {
                error = LogicCircuitValidationError.DuplicateNodeId;
                detail = node.Id;
                return false;
            }

            foreach (var config in node.Config)
            {
                if (config.Text != null && config.Text.Length > limits.MaxConfigLength)
                {
                    error = LogicCircuitValidationError.ConfigTooLong;
                    detail = node.Id;
                    return false;
                }
            }

            power += proto.Cost;
        }

        if (power > limits.PowerBudget)
        {
            error = LogicCircuitValidationError.NotEnoughPower;
            detail = $"{power}/{limits.PowerBudget}";
            return false;
        }

        var takenInputs = new HashSet<(string Node, int Pin)>(layout.Wires.Count);
        foreach (var wire in layout.Wires)
        {
            if (!nodes.TryGetValue(wire.From, out var from))
            {
                error = LogicCircuitValidationError.UnknownWireNode;
                detail = wire.From;
                return false;
            }

            if (!nodes.TryGetValue(wire.To, out var to))
            {
                error = LogicCircuitValidationError.UnknownWireNode;
                detail = wire.To;
                return false;
            }

            if (wire.FromPin < 0 || wire.FromPin >= from.Outputs.Count)
            {
                error = LogicCircuitValidationError.UnknownPin;
                detail = $"{wire.From}:{wire.FromPin}";
                return false;
            }

            if (wire.ToPin < 0 || wire.ToPin >= to.Inputs.Count)
            {
                error = LogicCircuitValidationError.UnknownPin;
                detail = $"{wire.To}:{wire.ToPin}";
                return false;
            }

            if (!takenInputs.Add((wire.To, wire.ToPin)))
            {
                error = LogicCircuitValidationError.InputAlreadyWired;
                detail = $"{wire.To}:{wire.ToPin}";
                return false;
            }
        }

        return true;
    }
}
