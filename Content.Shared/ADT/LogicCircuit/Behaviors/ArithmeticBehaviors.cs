namespace Content.Shared.ADT.LogicCircuit.Behaviors;

/// <summary>
/// Сложение.
/// </summary>
public sealed partial class AddBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, ctx.InputNumber(0) + ctx.InputNumber(1));
    }
}

/// <summary>
/// Вычитание.
/// </summary>
public sealed partial class SubtractBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, ctx.InputNumber(0) - ctx.InputNumber(1));
    }
}

/// <summary>
/// Умножение.
/// </summary>
public sealed partial class MultiplyBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, ctx.InputNumber(0) * ctx.InputNumber(1));
    }
}

/// <summary>
/// Деление.
/// </summary>
public sealed partial class DivideBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var divisor = ctx.InputNumber(1);
        ctx.OutputNumber(0, divisor == 0f ? 0f : ctx.InputNumber(0) / divisor);
    }
}

/// <summary>
/// Остаток от деления.
/// </summary>
public sealed partial class ModuloBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var divisor = ctx.InputNumber(1);
        ctx.OutputNumber(0, divisor == 0f ? 0f : ctx.InputNumber(0) % divisor);
    }
}

/// <summary>
/// Модуль числа.
/// </summary>
public sealed partial class AbsBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, MathF.Abs(ctx.InputNumber(0)));
    }
}

/// <summary>
/// Округление до заданного числа знаков после запятой.
/// </summary>
public sealed partial class RoundBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var digits = (int) ctx.SettingNumber(0);
        digits = Math.Clamp(digits, 0, 6);

        ctx.OutputNumber(0, MathF.Round(ctx.InputNumber(0), digits));
    }
}

/// <summary>
/// Меньшее из двух чисел.
/// </summary>
public sealed partial class MinBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, MathF.Min(ctx.InputNumber(0), ctx.InputNumber(1)));
    }
}

/// <summary>
/// Большее из двух чисел.
/// </summary>
public sealed partial class MaxBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, MathF.Max(ctx.InputNumber(0), ctx.InputNumber(1)));
    }
}
