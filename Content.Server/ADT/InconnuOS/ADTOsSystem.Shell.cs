using Content.Server.ADT.InconnuOS.Shell;
using Content.Shared.ADT.InconnuOS;
using Content.Shared.ADT.InconnuOS.Components;
using Robust.Shared.Reflection;

namespace Content.Server.ADT.InconnuOS;

public sealed partial class ADTOsSystem
{
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly IDynamicTypeFactory _types = default!;

    private readonly Dictionary<string, OsShellCommand> _commands = new();

    partial void InitializeShell()
    {
        SubscribeLocalEvent<ADTOperatingSystemComponent, ADTOsCommandMessage>(OnCommand);

        foreach (var type in _reflection.GetAllChildren<OsShellCommand>())
        {
            if (type.IsAbstract)
                continue;

            var command = (OsShellCommand) _types.CreateInstance(type);

            if (_commands.TryAdd(command.Name, command))
                continue;

            Log.Error($"Команда InconnuOS {command.Name} объявлена дважды.");
        }
    }

    private void OnCommand(Entity<ADTOperatingSystemComponent> ent, ref ADTOsCommandMessage args)
    {
        if (!CanOperate(ent))
            return;

        var comp = ent.Comp;
        var line = args.Line.Trim();

        var context = new OsShellContext(ent, args.Actor, comp.WorkingDirectory);

        if (line.Length > comp.MaxCommandLength)
        {
            context.Write(Loc.GetString("os-terminal-too-long", ("limit", comp.MaxCommandLength)));
            Reply(ent, args.Actor, context);
            return;
        }

        if (line.Length > 0)
            Run(context, line);

        comp.WorkingDirectory = context.WorkingDirectory;

        Reply(ent, args.Actor, context);
    }

    private void Run(OsShellContext context, string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var name = parts[0].ToLowerInvariant();

        if (!_commands.TryGetValue(name, out var command))
        {
            context.Write(Loc.GetString("os-terminal-unknown", ("command", parts[0])));
            return;
        }

        command.Execute(context, parts[1..]);
    }

    private void Reply(Entity<ADTOperatingSystemComponent> ent, EntityUid actor, OsShellContext context)
    {
        if (!actor.IsValid())
            return;

        _ui.ServerSendUiMessage(
            ent.Owner,
            ADTComputerUiKey.Key,
            new ADTOsOutputMessage(context.Output.ToArray(), context.WorkingDirectory),
            actor);
    }
}
