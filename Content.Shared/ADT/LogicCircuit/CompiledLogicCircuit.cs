using System.Runtime.InteropServices;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.LogicCircuit;

public sealed class CompiledLogicCircuit
{
    public readonly LogicNodeData[] Nodes;

    public readonly LogicElementPrototype[] Prototypes;
    public readonly LogicElementBehavior[] Behaviors;

    /// <summary>
    /// Префиксные суммы числа входных пинов.
    /// </summary>
    public readonly int[] InputOffsets;

    /// <summary>
    /// Префиксные суммы числа выходных пинов.
    /// </summary>
    public readonly int[] OutputOffsets;

    public readonly int[] InputSources;

    private readonly LogicSignal[] _inputs;

    public LogicSignal[] Prev;
    public LogicSignal[] Cur;

    public int NodeCount => Nodes.Length;
    public int PinCount => Prev.Length;

    private CompiledLogicCircuit(
        LogicNodeData[] nodes,
        LogicElementPrototype[] prototypes,
        LogicElementBehavior[] behaviors,
        int[] inputOffsets,
        int[] outputOffsets,
        int[] inputSources,
        int pinCount)
    {
        Nodes = nodes;
        Prototypes = prototypes;
        Behaviors = behaviors;
        InputOffsets = inputOffsets;
        OutputOffsets = outputOffsets;
        InputSources = inputSources;
        _inputs = new LogicSignal[inputSources.Length];
        Prev = new LogicSignal[pinCount];
        Cur = new LogicSignal[pinCount];
    }

    public static CompiledLogicCircuit Compile(LogicCircuitLayout layout, IPrototypeManager protoMan)
    {
        var count = layout.Nodes.Count;
        var nodes = new LogicNodeData[count];
        var prototypes = new LogicElementPrototype[count];
        var behaviors = new LogicElementBehavior[count];
        var inputOffsets = new int[count + 1];
        var outputOffsets = new int[count + 1];

        var indices = new Dictionary<string, int>(count);
        var written = 0;

        foreach (var node in layout.Nodes)
        {
            if (!protoMan.TryIndex(node.Proto, out var proto))
                continue;

            if (!indices.TryAdd(node.Id, written))
                continue;

            nodes[written] = node;
            prototypes[written] = proto;
            behaviors[written] = proto.Behavior;
            inputOffsets[written + 1] = inputOffsets[written] + proto.Inputs.Count;
            outputOffsets[written + 1] = outputOffsets[written] + proto.Outputs.Count;
            written++;
        }

        if (written != count)
        {
            Array.Resize(ref nodes, written);
            Array.Resize(ref prototypes, written);
            Array.Resize(ref behaviors, written);
            Array.Resize(ref inputOffsets, written + 1);
            Array.Resize(ref outputOffsets, written + 1);
        }

        var inputSources = new int[inputOffsets[written]];
        for (var i = 0; i < inputSources.Length; i++)
        {
            inputSources[i] = -1;
        }

        foreach (var wire in layout.Wires)
        {
            if (!indices.TryGetValue(wire.From, out var from) || !indices.TryGetValue(wire.To, out var to))
                continue;

            var fromPins = outputOffsets[from + 1] - outputOffsets[from];
            var toPins = inputOffsets[to + 1] - inputOffsets[to];

            if ((uint) wire.FromPin >= (uint) fromPins || (uint) wire.ToPin >= (uint) toPins)
                continue;

            inputSources[inputOffsets[to] + wire.ToPin] = outputOffsets[from] + wire.FromPin;
        }

        return new CompiledLogicCircuit(
            nodes,
            prototypes,
            behaviors,
            inputOffsets,
            outputOffsets,
            inputSources,
            outputOffsets[written]);
    }

    /// <summary>
    /// Считает один тик схемы.
    /// </summary>
    /// <param name="portInputs">Последние значения, принятые коробкой снаружи.</param>
    /// <param name="portOutputs">Куда схема складывает то, что надо отправить наружу.</param>
    /// <param name="deltaSeconds">Сколько прошло с прошлого тика этой схемы.</param>
    /// <param name="curTime">Игровое время.</param>
    /// <param name="maxSignalLength">Предел длины текстового сигнала.</param>
    /// <param name="wireless">Беспроводные каналы карты, на которой стоит коробка.</param>
    /// <param name="keepAwake">Попросил ли кто-то из элементов не усыплять схему.</param>
    /// <returns>Изменилось ли хоть одно значение на пинах.</returns>
    public bool Tick(
        ReadOnlySpan<LogicSignal> portInputs,
        Span<LogicSignal> portOutputs,
        float deltaSeconds,
        TimeSpan curTime,
        int maxSignalLength,
        Dictionary<string, LogicSignal>? wireless,
        out bool keepAwake)
    {
        Array.Copy(Prev, Cur, Prev.Length);

        for (var i = 0; i < InputSources.Length; i++)
        {
            var source = InputSources[i];
            _inputs[i] = source >= 0 ? Prev[source] : LogicSignal.Empty;
        }

        keepAwake = false;

        for (var i = 0; i < Nodes.Length; i++)
        {
            var node = Nodes[i];
            var inputStart = InputOffsets[i];
            var outputStart = OutputOffsets[i];

            var ctx = new LogicElementContext(
                _inputs.AsSpan(inputStart, InputOffsets[i + 1] - inputStart),
                Cur.AsSpan(outputStart, OutputOffsets[i + 1] - outputStart),
                node.State,
                CollectionsMarshal.AsSpan(node.Config),
                portInputs,
                portOutputs,
                deltaSeconds,
                curTime,
                maxSignalLength,
                wireless);

            Behaviors[i].Process(ref ctx);

            if (ctx.KeepAwakeRequested)
                keepAwake = true;
        }

        var changed = !Cur.AsSpan().SequenceEqual(Prev.AsSpan());

        (Prev, Cur) = (Cur, Prev);

        return changed;
    }
}
