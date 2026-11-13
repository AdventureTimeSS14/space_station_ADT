namespace Content.Shared.ADT.LogicCircuit.Behaviors;

/// <summary>
/// И: выход есть, только если есть оба входа.
/// </summary>
public sealed partial class AndBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputBool(0, ctx.InputBool(0) && ctx.InputBool(1));
    }
}

/// <summary>
/// ИЛИ: выход есть, если есть хотя бы один вход.
/// </summary>
public sealed partial class OrBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputBool(0, ctx.InputBool(0) || ctx.InputBool(1));
    }
}

/// <summary>
/// НЕ: выход есть, когда входа нет.
/// </summary>
public sealed partial class NotBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputBool(0, !ctx.InputBool(0));
    }
}

/// <summary>
/// Исключающее ИЛИ: выход есть, когда входы различаются.
/// </summary>
public sealed partial class XorBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputBool(0, ctx.InputBool(0) != ctx.InputBool(1));
    }
}
