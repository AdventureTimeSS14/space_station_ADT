namespace Content.Shared.ADT.LogicCircuit.Behaviors;

/// <summary>
/// Склейка
/// </summary>
public sealed partial class ConcatenationBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var a = ctx.Input(0).AsText();
        var b = ctx.Input(1).AsText();

        if (a.Length == 0 && b.Length == 0)
        {
            ctx.Output(0, LogicSignal.Empty);
            return;
        }

        ctx.Output(0, ctx.Text(a + ctx.Setting(0).AsText() + b));
    }
}
