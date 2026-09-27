namespace Content.Shared.ADT.LogicCircuit.Behaviors;

/// <summary>
/// Равенство. Два числа сравниваются с заданной точностью, всё остальное как текст.
/// </summary>
public sealed partial class EqualsBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var a = ctx.Input(0);
        var b = ctx.Input(1);

        if (a.IsNumber && b.IsNumber)
        {
            var epsilon = MathF.Abs(ctx.SettingNumber(0));
            ctx.OutputBool(0, MathF.Abs(a.Number - b.Number) <= epsilon);
            return;
        }

        ctx.OutputBool(0, string.Equals(a.AsText(), b.AsText(), StringComparison.Ordinal));
    }
}

/// <summary>
/// Больше.
/// </summary>
public sealed partial class GreaterBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputBool(0, ctx.InputNumber(0) > ctx.InputNumber(1));
    }
}

/// <summary>
/// Меньше.
/// </summary>
public sealed partial class LessBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputBool(0, ctx.InputNumber(0) < ctx.InputNumber(1));
    }
}
