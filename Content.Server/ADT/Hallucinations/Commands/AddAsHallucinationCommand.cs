using System.Linq;
using Content.Server.Administration;
using Content.Server.ADT.Hallucinations.Components;
using Content.Server.ADT.Hallucinations.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Hallucinations.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class AddAsHallucinationCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public string Command => "add-as-hallucination";

    public string Description => Loc.GetString("add-as-hallucination-command-description");

    public string Help => Loc.GetString("add-as-hallucination-command-help-text", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 4)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var target))
        {
            shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }
        if (!EntityUid.TryParse(args[1], out var toAdd))
        {
            shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        var hall = _entManager.System<SchizophreniaSystem>();
        hall.AddAsHallucination(target, toAdd);
        shell.WriteLine(Loc.GetString("add-as-hallucination-command-success", ("target", target), ("added", toAdd)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var opts = _entManager.AllEntities<CanHallucinateComponent>().Select(ent => new CompletionOption(ent.Owner.ToString(), _entManager.ToPrettyString(ent))).ToList();
            return CompletionResult.FromHintOptions(opts, "<target>");
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHint("<entity to add>");
        }

        return CompletionResult.Empty;
    }
}
