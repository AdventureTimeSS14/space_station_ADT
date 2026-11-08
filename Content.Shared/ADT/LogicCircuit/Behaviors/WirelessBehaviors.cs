namespace Content.Shared.ADT.LogicCircuit.Behaviors;

/// <summary>
/// Беспроводной передатчик
/// </summary>
public sealed partial class WirelessOutBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.WirelessSet(ctx.Setting(0).AsText(), ctx.Input(0));
    }
}

/// <summary>
/// Беспроводной приёмник
/// </summary>
public sealed partial class WirelessInBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.Output(0, ctx.WirelessGet(ctx.Setting(0).AsText()));
        ctx.KeepAwake();
    }
}
