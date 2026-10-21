using Content.Client.ADT.Drawing.Canvas;
using Content.Client.ADT.Drawing.UI;
using Robust.Shared.Console;

namespace Content.Client.ADT.Drawing;

public sealed class DrawingCommand : IConsoleCommand
{
    private const int DefaultSize = 2048;

    public string Command => "adt_draw";
    public string Description => "Открывает окно рисования (тест холста и графического планшета).";
    public string Help => $"Usage: {Command} [width] [height] (1..{PaintSurface.MaxSize}, default {DefaultSize})";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var width = DefaultSize;
        var height = DefaultSize;

        if (args.Length >= 1 && !int.TryParse(args[0], out width) ||
            args.Length >= 2 && !int.TryParse(args[1], out height))
        {
            shell.WriteError(Help);
            return;
        }

        if (args.Length == 1)
            height = width;

        if (width is < 1 or > PaintSurface.MaxSize || height is < 1 or > PaintSurface.MaxSize)
        {
            shell.WriteError(Help);
            return;
        }

        new DrawingWindow(width, height).OpenCentered();
    }
}
