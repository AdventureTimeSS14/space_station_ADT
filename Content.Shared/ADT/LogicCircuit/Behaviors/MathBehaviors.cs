namespace Content.Shared.ADT.LogicCircuit.Behaviors;

/// <summary>
/// Синус угла в градусах.
/// </summary>
public sealed partial class SinBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, MathF.Sin(ctx.InputNumber(0) * MathF.PI / 180f));
    }
}

/// <summary>
/// Косинус угла в градусах.
/// </summary>
public sealed partial class CosBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, MathF.Cos(ctx.InputNumber(0) * MathF.PI / 180f));
    }
}

/// <summary>
/// Тангенс угла в градусах.
/// </summary>
public sealed partial class TanBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, MathF.Tan(ctx.InputNumber(0) * MathF.PI / 180f));
    }
}

/// <summary>
/// Арксинус, результат в градусах.
/// </summary>
public sealed partial class AsinBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var value = Math.Clamp(ctx.InputNumber(0), -1f, 1f);
        ctx.OutputNumber(0, MathF.Asin(value) * 180f / MathF.PI);
    }
}

/// <summary>
/// Арккосинус, результат в градусах.
/// </summary>
public sealed partial class AcosBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var value = Math.Clamp(ctx.InputNumber(0), -1f, 1f);
        ctx.OutputNumber(0, MathF.Acos(value) * 180f / MathF.PI);
    }
}

/// <summary>
/// Арктангенс отношения, результат в градусах.
/// </summary>
public sealed partial class AtanBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, MathF.Atan2(ctx.InputNumber(0), ctx.InputNumber(1)) * 180f / MathF.PI);
    }
}

/// <summary>
/// Возведение в степень.
/// </summary>
public sealed partial class PowBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, MathF.Pow(ctx.InputNumber(0), ctx.InputNumber(1)));
    }
}

/// <summary>
/// Квадратный корень.
/// </summary>
public sealed partial class SqrtBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var value = ctx.InputNumber(0);
        ctx.OutputNumber(0, value <= 0f ? 0f : MathF.Sqrt(value));
    }
}

/// <summary>
/// Натуральный логарифм.
/// </summary>
public sealed partial class LogBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var value = ctx.InputNumber(0);
        ctx.OutputNumber(0, value <= 0f ? 0f : MathF.Log(value));
    }
}

/// <summary>
/// Экспонента.
/// </summary>
public sealed partial class ExpBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, MathF.Exp(ctx.InputNumber(0)));
    }
}

/// <summary>
/// Округление вниз.
/// </summary>
public sealed partial class FloorBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, MathF.Floor(ctx.InputNumber(0)));
    }
}

/// <summary>
/// Округление вверх.
/// </summary>
public sealed partial class CeilBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, MathF.Ceiling(ctx.InputNumber(0)));
    }
}

/// <summary>
/// Ограничение числа заданными границами.
/// </summary>
public sealed partial class ClampBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var min = ctx.SettingNumber(0);
        var max = ctx.SettingNumber(1, 1f);

        if (min > max)
            (min, max) = (max, min);

        ctx.OutputNumber(0, Math.Clamp(ctx.InputNumber(0), min, max));
    }
}
