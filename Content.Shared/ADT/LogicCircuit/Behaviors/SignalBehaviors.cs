namespace Content.Shared.ADT.LogicCircuit.Behaviors;

public sealed partial class SignalInBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var value = ctx.PortInput((int) ctx.SettingNumber(0, 1f));
        var replacement = ctx.Setting(1);

        if (replacement.IsEmpty)
        {
            ctx.Output(0, value);
            return;
        }

        ctx.Output(0, value.AsBool() ? replacement : LogicSignal.Empty);
    }
}

public sealed partial class SignalOutBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.PortOutput((int) ctx.SettingNumber(0, 1f), ctx.Input(0));
    }
}

public sealed partial class DisplayBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.Output(0, ctx.Input(0));
    }
}
