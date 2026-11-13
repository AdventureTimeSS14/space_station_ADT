using System.Runtime.CompilerServices;

namespace Content.Shared.ADT.LogicCircuit;

public ref struct LogicElementContext
{
    public readonly ReadOnlySpan<LogicSignal> Inputs;

    public readonly Span<LogicSignal> Outputs;
    public readonly Span<LogicSignal> State;

    public readonly ReadOnlySpan<LogicSignal> Config;
    public readonly ReadOnlySpan<LogicSignal> PortInputs;

    public readonly Span<LogicSignal> PortOutputs;

    public readonly float DeltaSeconds;

    public readonly TimeSpan CurTime;

    public readonly int MaxSignalLength;

    public readonly Dictionary<string, LogicSignal>? Wireless;

    public bool KeepAwakeRequested { get; private set; }

    public LogicElementContext(
        ReadOnlySpan<LogicSignal> inputs,
        Span<LogicSignal> outputs,
        Span<LogicSignal> state,
        ReadOnlySpan<LogicSignal> config,
        ReadOnlySpan<LogicSignal> portInputs,
        Span<LogicSignal> portOutputs,
        float deltaSeconds,
        TimeSpan curTime,
        int maxSignalLength,
        Dictionary<string, LogicSignal>? wireless)
    {
        Inputs = inputs;
        Outputs = outputs;
        State = state;
        Config = config;
        PortInputs = portInputs;
        PortOutputs = portOutputs;
        DeltaSeconds = deltaSeconds;
        CurTime = curTime;
        MaxSignalLength = maxSignalLength;
        Wireless = wireless;
        KeepAwakeRequested = false;
    }

    public void KeepAwake()
    {
        KeepAwakeRequested = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LogicSignal Input(int index)
    {
        return (uint) index < (uint) Inputs.Length ? Inputs[index] : LogicSignal.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float InputNumber(int index)
    {
        return Input(index).AsNumber();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool InputBool(int index)
    {
        return Input(index).AsBool();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Output(int index, LogicSignal value)
    {
        if ((uint) index < (uint) Outputs.Length)
            Outputs[index] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OutputNumber(int index, float value)
    {
        Output(index, LogicSignal.FromNumber(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OutputBool(int index, bool value)
    {
        Output(index, LogicSignal.FromBool(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LogicSignal Text(string? value)
    {
        return LogicSignal.FromText(value, MaxSignalLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LogicSignal Setting(int index)
    {
        return (uint) index < (uint) Config.Length ? Config[index] : LogicSignal.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float SettingNumber(int index, float fallback = 0f)
    {
        var signal = Setting(index);
        return signal.IsNumber ? signal.Number : fallback;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SettingBool(int index)
    {
        return Setting(index).AsBool();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LogicSignal GetState(int index)
    {
        return (uint) index < (uint) State.Length ? State[index] : LogicSignal.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetState(int index, LogicSignal value)
    {
        if ((uint) index < (uint) State.Length)
            State[index] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LogicSignal PortInput(int port)
    {
        var index = port - 1;
        return (uint) index < (uint) PortInputs.Length ? PortInputs[index] : LogicSignal.Empty;
    }

    public LogicSignal WirelessGet(string channel)
    {
        if (Wireless == null || channel.Length == 0)
            return LogicSignal.Empty;

        return Wireless.TryGetValue(channel, out var value) ? value : LogicSignal.Empty;
    }

    public void WirelessSet(string channel, LogicSignal value)
    {
        if (Wireless == null || channel.Length == 0)
            return;

        Wireless[channel] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PortOutput(int port, LogicSignal value)
    {
        var index = port - 1;
        if ((uint) index < (uint) PortOutputs.Length)
            PortOutputs[index] = value;
    }
}
