using Content.Server.Administration;
using Content.Server.Commands;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.GhostKick;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Mind;
using Content.Shared.Administration;
using Content.Shared.Mind.Components;
using Robust.Server.Player;
using Robust.Shared.Console;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Mind;
using Content.Shared.Administration;
using Content.Shared.Mind.Components;
using Robust.Server.Player;
using Robust.Shared.Console;
using Content.Shared.Movement.Components;
using Content.Client.Administration.Components;
using Robust.Server.GameObjects;


namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class ZoomTweakCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;


    public override string Command => "zoom_tweak";
    public override string Description => Loc.GetString("zoom_tweak-command-description");

    public override string Help => Loc.GetString("zoom_tweak-command-help-text", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine("Error: invalid arguments!");
            return;
        }

        // EntityUid Player
        if (!TryParseUid(args[0], shell, _entManager, out var entityUidPlayer))
            return;

        if (_entManager.TryGetComponent<ContentEyeComponent>(entityUidPlayer, out var contentEyeComponent))
        {
            _entManager.System<ContentEyeSystem>().RequestZoom(player.Value, zoom, true, scalePvs, content);
            contentEyeComponent.TargetZoom = 
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
