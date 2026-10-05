using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.ADT.LinkedBodies;

[AdminCommand(AdminFlags.Fun)]
public sealed class LinkBodiesCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ADTLinkedBodiesSystem _linkedBodies = default!;

    public override string Command => "linkbodies";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-arguments", ("minimum", 2)));
            shell.WriteLine(Help);
            return;
        }

        var bodies = new HashSet<EntityUid>();
        foreach (var arg in args)
        {
            if (!NetEntity.TryParse(arg, out var netEntity) || !EntityManager.TryGetEntity(netEntity, out var body))
            {
                shell.WriteError(Loc.GetString("shell-invalid-entity-uid", ("uid", arg)));
                return;
            }

            bodies.Add(body.Value);
        }

        if (bodies.Count < 2)
        {
            shell.WriteError(Loc.GetString("cmd-linkbodies-same"));
            return;
        }

        _linkedBodies.Link(bodies);
        shell.WriteLine(Loc.GetString("cmd-linkbodies-success", ("count", bodies.Count)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.FromHint(Loc.GetString("cmd-linkbodies-hint"));
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class UnlinkBodiesCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ADTLinkedBodiesSystem _linkedBodies = default!;

    public override string Command => "unlinkbodies";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-arguments", ("minimum", 1)));
            shell.WriteLine(Help);
            return;
        }

        foreach (var arg in args)
        {
            if (!NetEntity.TryParse(arg, out var netEntity) || !EntityManager.TryGetEntity(netEntity, out var body))
            {
                shell.WriteError(Loc.GetString("shell-invalid-entity-uid", ("uid", arg)));
                continue;
            }

            _linkedBodies.Unlink(body.Value);
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.FromHint(Loc.GetString("cmd-linkbodies-hint"));
    }
}
