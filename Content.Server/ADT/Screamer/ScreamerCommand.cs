using System.Linq;
using System.Numerics;
using Content.Server.ADT.Shizophrenia;
using Content.Shared.Administration;
using Content.Shared.ADT.Screamer;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
sealed class ScreamerCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    public string Command => "screamer";

    public string Description => Loc.GetString("screamer-command-description");

    public string Help => Loc.GetString("screamer-command-help-text", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 3)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var uid))
        {
            shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!_proto.HasIndex(args[1]))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
            return;
        }

        if (!int.TryParse(args[2], out var duration))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-integer", ("arg", args[2])));
            return;
        }

        string? sound = null;
        var alpha = 0.5f;
        var fadeIn = false;
        var fadeOut = false;
        var offset = Vector2.Zero;

        if (args.Length > 3)
        {
            if (args[3] == "null")
                sound = null;
            else
                sound = args[3];
        }


        if (args.Length > 4)
        {
            if (!float.TryParse(args[4], out alpha))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[4])));
                return;
            }
            else if (alpha > 1 || alpha < 0)
            {
                alpha = 0.5f;
            }
        }

        if (args.Length > 5)
        {
            if (!bool.TryParse(args[5], out fadeIn))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-bool", ("arg", args[5])));
                return;
            }
        }

        if (args.Length > 6)
        {
            if (!bool.TryParse(args[6], out fadeOut))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-bool", ("arg", args[6])));
                return;
            }
        }

        if (args.Length > 7)
        {
            if (!float.TryParse(args[7], out var x))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[7])));
                return;
            }

            offset.X = x;
        }

        if (args.Length > 8)
        {
            if (!float.TryParse(args[8], out var y))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[8])));
                return;
            }

            offset.Y = y;
        }

        if (!_player.TryGetSessionByEntity(uid, out var session))
            return;

        var msg = new DoScreamerMessage(args[1], sound, offset, alpha, duration, fadeIn, fadeOut);
        _entManager.EntityNetManager?.SendSystemNetworkMessage(msg, session.Channel);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var opts = _entManager.AllEntities<ScreamersComponent>().Select(ent => new CompletionOption(ent.Owner.ToString(), _entManager.ToPrettyString(ent))).ToList();
            return CompletionResult.FromHintOptions(opts, "<target>");
        }

        if (args.Length == 2)
        {
            var opts = _proto.EnumeratePrototypes<EntityPrototype>().Where(x => x.Categories.Count > 0 && x.Categories.First().ID == "Screamers").Select(proto => proto.ID).ToList();
            return CompletionResult.FromHintOptions(opts, "<prototype>");
        }

        if (args.Length == 3)
            return CompletionResult.FromHint("<duration>");

        if (args.Length == 4)
        {
            var hint = Loc.GetString("play-global-sound-command-arg-path");

            var options = CompletionHelper.AudioFilePath(args[4], _proto, _res);

            return CompletionResult.FromHintOptions(options, hint);
        }

        if (args.Length == 5)
            return CompletionResult.FromHintOptions(new List<string>() { "0.5" }, "<alpha>");

        if (args.Length == 6)
            return CompletionResult.FromHintOptions(new List<string>() { "true", "false" }, "<fade in>");

        if (args.Length == 7)
            return CompletionResult.FromHintOptions(new List<string>() { "true", "false" }, "<fade out>");

        if (args.Length == 8)
            return CompletionResult.FromHint("<offset x>");

        if (args.Length == 9)
            return CompletionResult.FromHint("<offset y>");

        return CompletionResult.Empty;
    }
}
