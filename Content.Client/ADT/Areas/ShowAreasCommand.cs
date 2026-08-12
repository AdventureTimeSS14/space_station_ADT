using Robust.Shared.Console;

namespace Content.Client.ADT.Areas;

public sealed class ShowAreasCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "showareas";
    public string Description => "Shows area markers.";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var areas = _entities.System<AreasCommandSystem>();
        areas.Enabled = !areas.Enabled;
        shell.WriteLine($"Areas visible: {areas.Enabled}");
    }
}
