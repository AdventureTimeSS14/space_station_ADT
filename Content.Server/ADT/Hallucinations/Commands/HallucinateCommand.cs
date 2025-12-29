using System.Linq;
using Content.Server.ADT.Shizophrenia;
using Content.Shared.Administration;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
sealed class HallucinateCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public string Command => "hallucinate";

    public string Description => Loc.GetString("hallucinate-command-description", ("requiredComponent", nameof(MindContainerComponent)));

    public string Help => Loc.GetString("hallucinate-command-help-text", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 4)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var uid))
        {
            shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!Enum.TryParse<HallucinationsMetabolismType>(args[1], out var type))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-metabolism-type"));
            return;
        }

        if (!int.TryParse(args[2], out var duration))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-integer", ("arg", args[2])));
            return;
        }

        var hall = _entManager.System<SchizophreniaSystem>();

        for (var i = 3; i < args.Length; i++)
        {
            if (!_proto.HasIndex<HallucinationsPackPrototype>(args[i]))
                shell.WriteLine($"Invalid pack: {args[i]}");
            else
                hall.AddOrAdjustHallucinations(uid, args[i], duration, type);
        }
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
            var opts = new List<string>()
            {
                "Add",
                "Set",
                "Remove"
            };
            return CompletionResult.FromHintOptions(opts, "<type>");
        }

        if (args.Length == 3)
            return CompletionResult.FromHint("<duration>");

        var packs = _proto.EnumeratePrototypes<HallucinationsPackPrototype>().Select(pack => new CompletionOption(pack.ID, pack.ID)).ToList();
        return CompletionResult.FromHintOptions(packs, "<pack>");
    }
}
