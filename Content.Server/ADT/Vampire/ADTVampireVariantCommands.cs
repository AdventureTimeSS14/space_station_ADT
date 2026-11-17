using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.ADT.Vampire;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Vampire;

[AdminCommand(AdminFlags.Admin)]
public sealed class VampireVariantCommand : LocalizedEntityCommands
{
    [Dependency] private VampireSystem _vampire = default!;

    public override string Command => "vampire_variant";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("cmd-vampire_variant-error-args"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var uid) || !EntityManager.EntityExists(uid))
        {
            shell.WriteError(Loc.GetString("cmd-vampire_variant-error-target"));
            return;
        }

        VampireVariant? variant = args[1].ToLowerInvariant() switch
        {
            "classic" => VampireVariant.Classic,
            "goon" => VampireVariant.Goon,
            "devil" => VampireVariant.Devil,
            _ => null
        };

        if (variant == null)
        {
            shell.WriteError(Loc.GetString("cmd-vampire_variant-error-variant"));
            return;
        }

        if (!_vampire.SetVariant(uid, variant.Value))
        {
            shell.WriteError(Loc.GetString("cmd-vampire_variant-error-target"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-vampire_variant-success", ("entity", uid), ("variant", args[1])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                new[] { "classic", "goon", "devil" },
                Loc.GetString("cmd-vampire_variant-hint-variant"));
        }

        return CompletionResult.FromHint(Loc.GetString("cmd-vampire_variant-hint-target"));
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class VampireAncientCommand : LocalizedEntityCommands
{
    [Dependency] private VampireSystem _vampire = default!;

    public override string Command => "vampire_ancient";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-vampire_ancient-error-args"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var uid) || !EntityManager.EntityExists(uid))
        {
            shell.WriteError(Loc.GetString("cmd-vampire_ancient-error-target"));
            return;
        }

        if (!_vampire.MakeAncient(uid))
        {
            shell.WriteError(Loc.GetString("cmd-vampire_ancient-error-target"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-vampire_ancient-success", ("entity", uid)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.FromHint(Loc.GetString("cmd-vampire_ancient-hint-target"));
    }
}