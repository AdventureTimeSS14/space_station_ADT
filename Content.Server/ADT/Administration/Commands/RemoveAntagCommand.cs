using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles;
using Robust.Shared.Console;
using Robust.Server.Player;
using Content.Server.Antag;

using Content.Server.Administration.Commands;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Zombies;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Content.Server.Roles;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed class RemoveAntagCommand : LocalizedEntityCommands
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedObjectivesSystem _sharedObjectivesSystem = default!;
    [Dependency] private readonly SharedRoleSystem _sharedRoleSystem = default!;
    [Dependency] private readonly SharedMindSystem _sharedMind = default!;

    public override string Command => "rmantag";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(@"Expected 1 or more arguments: <username> <RoleComponent>
            Available roles to delete: Traitor, Heretic, Nukeops, Revolutionary, Thief");
            return;
        }

        if (!_playerManager.TryGetSessionByUsername(args[0], out var session))
        {
            shell.WriteError("Can't find the player session.");
            return;
        }

        if (!_sharedMind.TryGetMind(session, out var mindId, out var mind))
        {
            shell.WriteError("Can't find the mind.");
            return;
        }

        var objectives = mind.Objectives.ToList();
        foreach (var objective in objectives)
        {
            _sharedMind.TryRemoveObjective(mindId, mind, 0);
        }
        shell.WriteLine("All objectives successfully removed!");

        string roleComp = args[1];
        var type = Type.GetType(roleComp);

        bool success = false;



        switch (roleComp)
        {
            case "TraitorRoleComponent":  // также удаляет аплинк встроенный в кпк синдиката
                success = _sharedRoleSystem.MindTryRemoveRole<TraitorRoleComponent>(mindId);
                break;
            case "HereticRoleComponent":
                success = _sharedRoleSystem.MindTryRemoveRole<HereticRoleComponent>(mindId);
                break;
            case "NukeopsRoleComponent":
                success = _sharedRoleSystem.MindTryRemoveRole<NukeopsRoleComponent>(mindId);
                break;
            case "RevolutionaryRoleComponent":
                success = _sharedRoleSystem.MindTryRemoveRole<RevolutionaryRoleComponent>(mindId);
                break;
            case "ThiefRoleComponent":
                success = _sharedRoleSystem.MindTryRemoveRole<ThiefRoleComponent>(mindId);
                break;
            case "ChangelingRoleComponent": // после этого остаются actions, включая магазин
                success = _sharedRoleSystem.MindTryRemoveRole<ChangelingRoleComponent>(mindId);
                break;
            default:
                shell.WriteLine($"roleComp does not exist or its deletion is not implemented");
                break;

        }



        if (success)
            shell.WriteLine($"{roleComp} role successfully removed!");
        else
            shell.WriteLine($"Error while deleting {roleComp} role!");

        // TODO: удаление Actions

        // TODO: Нужно дописать, чтобы удалялись компоненты у антагонистов
        // Если это еретик, то должны удаляться компоненты еретика и его магазин
        // Если генокрад, то всё тоже самое кмпонент Генокрада и его магазин удалить
        // Вор
        // агент синдиката, если есть аплинк имплантом или в кпк можно компонент магазина синдиката
        //
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(),
                Loc.GetString("cmd-mind-command-hint")
            );
        }

        return CompletionResult.Empty;
    }
}
