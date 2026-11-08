using Robust.Shared.Serialization;

namespace Content.Shared.ADT.LogicCircuit;

[Serializable, NetSerializable]
public enum ADTLogicCircuitUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public struct LogicCircuitLimits
{
    public int MaxNodes;
    public int MaxWires;
    public int MaxSignalLength;
    public int MaxConfigLength;
    public int PowerBudget;
}

[Serializable, NetSerializable]
public sealed class ADTLogicCircuitBuiState : BoundUserInterfaceState
{
    public readonly LogicCircuitLayout Layout;
    public readonly LogicCircuitLimits Limits;
    public readonly int PowerUsed;
    public readonly int PortCountIn;
    public readonly int PortCountOut;
    public readonly bool Enabled;
    public readonly bool Broken;

    public ADTLogicCircuitBuiState(
        LogicCircuitLayout layout,
        LogicCircuitLimits limits,
        int powerUsed,
        int portCountIn,
        int portCountOut,
        bool enabled,
        bool broken)
    {
        Layout = layout;
        Limits = limits;
        PowerUsed = powerUsed;
        PortCountIn = portCountIn;
        PortCountOut = portCountOut;
        Enabled = enabled;
        Broken = broken;
    }
}

[Serializable, NetSerializable]
public sealed class ADTLogicCircuitApplyMessage : BoundUserInterfaceMessage
{
    public readonly LogicCircuitLayout Layout;

    public ADTLogicCircuitApplyMessage(LogicCircuitLayout layout)
    {
        Layout = layout;
    }
}

[Serializable, NetSerializable]
public sealed class ADTLogicCircuitSetEnabledMessage : BoundUserInterfaceMessage
{
    public readonly bool Enabled;

    public ADTLogicCircuitSetEnabledMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class ADTLogicCircuitValuesMessage : BoundUserInterfaceMessage
{
    public readonly LogicSignal[] PinValues;

    public ADTLogicCircuitValuesMessage(LogicSignal[] pinValues)
    {
        PinValues = pinValues;
    }
}
