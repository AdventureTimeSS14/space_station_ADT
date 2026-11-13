using System.Linq;
using Content.Server.ADT.Body.Bones;
using Content.Server.ADT.Body.Shrapnel;
using Content.Server.Administration;
using Content.Shared.ADT.Body.Bones;
using Content.Shared.Administration;
using Content.Shared.Body;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class FractureCommand : SurgeryCommandBase
{
    public override string Command => "fracture";

    public override string Description => Loc.GetString("cmd-fracture-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!TryParseEntity(shell, args[0], out var uid))
            return;

        if (!Proto.TryIndex<ADTFractureTypePrototype>(args[2], out var type))
        {
            shell.WriteLine(Loc.GetString("cmd-fracture-unknown-type"));
            return;
        }

        var fractures = EntityManager.System<ADTFractureSystem>();
        if (!fractures.TryGetBone(uid, args[1], out var bone) || !fractures.ApplyFracture(uid, bone.Value, type))
        {
            shell.WriteLine(Loc.GetString("cmd-fracture-failed"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-fracture-done"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.Components<ADTBonesComponent>(args[0], EntityManager), Loc.GetString("cmd-organs-hint-entity")),
            2 => CompletionResult.FromHintOptions(PresentCategories<ADTBoneComponent>(args[0]), Loc.GetString("cmd-fracture-hint-category")),
            3 => CompletionResult.FromHintOptions(Proto.EnumeratePrototypes<ADTFractureTypePrototype>().Select(x => x.ID), Loc.GetString("cmd-fracture-hint-type")),
            _ => CompletionResult.Empty,
        };
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class MendBonesCommand : SurgeryCommandBase
{
    public override string Command => "mendbones";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        return CompletionResult.FromHintOptions(CompletionHelper.Components<ADTFracturedComponent>(args[0], EntityManager), Loc.GetString("cmd-organs-hint-entity"));
    }

    public override string Description => Loc.GetString("cmd-mendbones-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!TryParseEntity(shell, args[0], out var uid))
            return;

        EntityManager.System<ADTFractureSystem>().MendAll(uid);
        shell.WriteLine(Loc.GetString("cmd-mendbones-done"));
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class EmbedShrapnelCommand : SurgeryCommandBase
{
    public override string Command => "embedshrapnel";

    public override string Description => Loc.GetString("cmd-embedshrapnel-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!TryParseEntity(shell, args[0], out var uid))
            return;

        var count = 1;
        if (args.Length == 3 && !int.TryParse(args[2], out count))
        {
            shell.WriteLine(Loc.GetString("shell-argument-must-be-number"));
            return;
        }

        var shrapnel = EntityManager.System<ADTShrapnelSystem>();
        var embedded = 0;
        for (var i = 0; i < count; i++)
        {
            if (shrapnel.Embed(uid, args[1], "ADTShrapnel", true))
                embedded++;
        }

        shell.WriteLine(Loc.GetString("cmd-embedshrapnel-done", ("count", embedded)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => BodyCompletion(args[0]),
            2 => CompletionResult.FromHintOptions(PresentCategories<OrganComponent>(args[0]), Loc.GetString("cmd-fracture-hint-category")),
            3 => CompletionResult.FromHintOptions(new[] { "1", "3", "5" }, Loc.GetString("cmd-embedshrapnel-hint-count")),
            _ => CompletionResult.Empty,
        };
    }
}
