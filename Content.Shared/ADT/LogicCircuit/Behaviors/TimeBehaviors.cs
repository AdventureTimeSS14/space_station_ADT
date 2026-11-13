namespace Content.Shared.ADT.LogicCircuit.Behaviors;

/// <summary>
/// Задержка
/// </summary>
public sealed partial class DelayBehavior : LogicElementBehavior
{
    private const int StatePending = 0;
    private const int StateRemaining = 1;
    private const int StateLastInput = 2;

    public override void Process(ref LogicElementContext ctx)
    {
        var input = ctx.Input(0);

        if (input != ctx.GetState(StateLastInput))
        {
            ctx.SetState(StateLastInput, input);

            var delay = MathF.Max(0f, ctx.SettingNumber(0, 1f));

            if (delay <= 0f)
            {
                ctx.Output(0, input);
                ctx.SetState(StateRemaining, LogicSignal.Empty);
                return;
            }

            ctx.SetState(StatePending, input);
            ctx.SetState(StateRemaining, LogicSignal.FromNumber(delay));
            ctx.KeepAwake();
            return;
        }

        var remaining = ctx.GetState(StateRemaining).AsNumber();

        if (remaining <= 0f)
            return;

        remaining -= ctx.DeltaSeconds;

        if (remaining <= 0f)
        {
            ctx.SetState(StateRemaining, LogicSignal.Empty);
            ctx.Output(0, ctx.GetState(StatePending));
            return;
        }

        ctx.SetState(StateRemaining, LogicSignal.FromNumber(remaining));
        ctx.KeepAwake();
    }
}

/// <summary>
/// Генератор. Выдает импульс с заданной частотой.
/// </summary>
public sealed partial class OscillatorBehavior : LogicElementBehavior
{
    private const float MinFrequency = 0.05f;
    private const float MaxFrequency = 10f;

    public override void Process(ref LogicElementContext ctx)
    {
        var enable = ctx.Input(0);

        if (!enable.IsEmpty && !enable.AsBool())
        {
            ctx.SetState(0, LogicSignal.Empty);
            ctx.OutputBool(0, false);
            return;
        }

        var frequency = Math.Clamp(ctx.SettingNumber(0, 1f), MinFrequency, MaxFrequency);
        var phase = ctx.GetState(0).AsNumber() + ctx.DeltaSeconds * frequency;
        phase -= MathF.Floor(phase);

        ctx.SetState(0, LogicSignal.FromNumber(phase));
        ctx.OutputBool(0, phase < 0.5f);
        ctx.KeepAwake();
    }
}
