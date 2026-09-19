namespace Content.Shared.ADT.LogicCircuit.Behaviors;

/// <summary>
/// Постоянное значение из настроек элемента.
/// </summary>
public sealed partial class ConstantBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.Output(0, ctx.Setting(0));
    }
}

/// <summary>
/// Ячейка памяти
/// </summary>
public sealed partial class MemoryBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        if (ctx.InputBool(1))
            ctx.SetState(0, ctx.Input(0));

        ctx.Output(0, ctx.GetState(0));
    }
}

/// <summary>
/// Сдвиговый регистр
/// </summary>
public sealed partial class ShiftRegisterBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var push = ctx.InputBool(1);

        if (ctx.InputBool(2))
        {
            ctx.SetState(0, LogicSignal.Empty);
        }
        else if (push && !ctx.GetState(1).AsBool())
        {
            var max = (int) ctx.SettingNumber(0, 4f);
            max = Math.Clamp(max, 1, ctx.MaxSignalLength);

            var text = ctx.GetState(0).AsText() + ctx.Input(0).AsText();

            if (text.Length > max)
                text = text[^max..];

            ctx.SetState(0, ctx.Text(text));
        }

        ctx.SetState(1, LogicSignal.FromBool(push));
        ctx.Output(0, ctx.GetState(0));
    }
}

/// <summary>
/// Переключатель
/// </summary>
public sealed partial class ToggleBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var toggle = ctx.InputBool(0);

        if (ctx.InputBool(1))
        {
            ctx.SetState(0, LogicSignal.False);
        }
        else if (toggle && !ctx.GetState(1).AsBool())
        {
            ctx.SetState(0, LogicSignal.FromBool(!ctx.GetState(0).AsBool()));
        }

        ctx.SetState(1, LogicSignal.FromBool(toggle));
        ctx.Output(0, ctx.GetState(0));
    }
}

/// <summary>
/// Детектор фронта. Даёт импульс длиной в один тик на появление и на пропадание сигнала.
/// </summary>
public sealed partial class EdgeDetectorBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var now = ctx.InputBool(0);
        var previous = ctx.GetState(0).AsBool();

        ctx.OutputBool(0, now && !previous);
        ctx.OutputBool(1, !now && previous);

        ctx.SetState(0, LogicSignal.FromBool(now));
    }
}
