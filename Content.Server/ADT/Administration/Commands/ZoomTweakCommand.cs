using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Server.Administration;
using Content.Server.Movement.Systems;
using Content.Shared.Administration;
using Content.Shared.Movement.Components;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class ZoomTweakCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;


    public override string Command => "zoom_tweak";
    public override string Description => Loc.GetString("cmd-zoom_tweak-command-description", ("component", nameof(ContentEyeComponent)));

    public override string Help => Loc.GetString("cmd-zoom_tweak-command-help-text", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        Vector2 zoom = new(1f, 1f);
        if (args.Length != 2)
        {
            shell.WriteLine("Error: invalid arguments!");
            return;
        }

        // EntityUid Player
        if (!TryParseUid(args[0], shell, _entManager, out var entityUidPlayer))
            return;

        if (!float.TryParse(args[1], out var arg1))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[1])));
            return;
        }

        if (arg1 > 0)
            zoom = new(arg1, arg1);

        if (_entManager.TryGetComponent<ContentEyeComponent>(entityUidPlayer, out var contentEyeComponent))
        {
            _entManager.System<ContentEyeSystem>().SetZoom(entityUidPlayer.Value, zoom, true, contentEyeComponent);
            _entManager.System<ContentEyeSystem>().SetMaxZoom(entityUidPlayer.Value, zoom, contentEyeComponent);
        }
        else
        {
            shell.WriteError(Loc.GetString("cmd-zoom_tweak-command-error-content-eye", ("component", nameof(ContentEyeComponent))));
            return;
        }
    }

    private bool TryParseUid(string str, IConsoleShell shell,
        IEntityManager entMan, [NotNullWhen(true)] out EntityUid? entityUid)
    {
        entityUid = null;

        if (NetEntity.TryParse(str, out var entityUidNet) &&
            _entManager.TryGetEntity(entityUidNet, out entityUid) &&
            entMan.EntityExists(entityUid))
        {
            return true;
        }

        if (!_playerManager.TryGetSessionByUsername(str, out var session))
        {
            shell.WriteError(Loc.GetString("cmd-rename-not-found", ("target", str)));
            return false;
        }

        if (session.AttachedEntity == null)
        {
            shell.WriteError(Loc.GetString("cmd-rename-no-entity", ("target", str)));
            return false;
        }

        entityUid = session.AttachedEntity.Value;
        return true;
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), LocalizationManager.GetString("shell-argument-username-hint"));
        }

        return CompletionResult.Empty;
    }
}

/*
        ╔════════════════════════════════════╗
        ║   Schrödinger's Cat Code   🐾      ║
        ║   /\_/\\                           ║
        ║  ( o.o )  Meow!                    ║
        ║   > ^ <                            ║
        ╚════════════════════════════════════╝

*/
