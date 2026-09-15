namespace Content.Shared.ADT.LogicCircuit.Behaviors;

/// <summary>
/// Выбор. Пропускает одно из двух значений в зависимости от условия.
/// </summary>
public sealed partial class SelectBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.Output(0, ctx.InputBool(0) ? ctx.Input(1) : ctx.Input(2));
    }
}

/// <summary>
/// Таблица. По номеру на входе выдаёт одну из записей, перечисленных в настройках через
/// вертикальную черту.
/// </summary>
public sealed partial class LookupBehavior : LogicElementBehavior
{
    public const char Separator = '|';

    public override void Process(ref LogicElementContext ctx)
    {
        var index = (int) MathF.Floor(ctx.InputNumber(0));
        var table = ctx.Setting(0).AsText();

        if (table.Length == 0)
        {
            ctx.Output(0, LogicSignal.Empty);
            return;
        }

        var count = Count(table);
        var wrapped = ((index % count) + count) % count;

        ctx.Output(0, ctx.Text(GetEntry(table, wrapped)));
    }

    private static int Count(string table)
    {
        var count = 1;

        foreach (var symbol in table)
        {
            if (symbol == Separator)
                count++;
        }

        return count;
    }

    private static string GetEntry(string table, int index)
    {
        var start = 0;
        var current = 0;

        for (var i = 0; i <= table.Length; i++)
        {
            if (i != table.Length && table[i] != Separator)
                continue;

            if (current == index)
                return table[start..i];

            current++;
            start = i + 1;
        }

        return string.Empty;
    }
}

/// <summary>
/// Подстрока
/// </summary>
public sealed partial class SubstringBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        var text = ctx.Input(0).AsText();
        var start = Math.Clamp((int) ctx.SettingNumber(0), 0, text.Length);
        var length = Math.Clamp((int) ctx.SettingNumber(1, 1f), 0, text.Length - start);

        ctx.Output(0, length == 0 ? LogicSignal.Empty : ctx.Text(text.Substring(start, length)));
    }
}

/// <summary>
/// Длина значения в символах.
/// </summary>
public sealed partial class LengthBehavior : LogicElementBehavior
{
    public override void Process(ref LogicElementContext ctx)
    {
        ctx.OutputNumber(0, ctx.Input(0).AsText().Length);
    }
}
