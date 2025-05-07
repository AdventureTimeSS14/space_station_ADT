using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles;
using Robust.Shared.Console;
using Robust.Server.Player;
using Content.Server.Antag;
using Content.Server.Roles;
using Content.Shared.Actions;
using Content.Shared.Changeling.Components;
using Content.Shared.Zombies;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed class RemoveAntagCommand : LocalizedEntityCommands
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedObjectivesSystem _sharedObjectivesSystem = default!;
    [Dependency] private readonly SharedRoleSystem _sharedRoleSystem = default!;
    [Dependency] private readonly SharedActionsSystem _sharedActionsSystem = default!;
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

        string role = args[1];
        var type = Type.GetType(role);

        bool success = false;

        EntityUid antagCharacterEntity = EntityUid.Invalid;
        if (_entityManager.TryGetComponent<MindComponent>(uid: mindId, out var mindComponent))
        {
            antagCharacterEntity = mindComponent.OwnedEntity.HasValue ? mindComponent.OwnedEntity.Value : EntityUid.Invalid;
        }

        var roleRemovers = new Dictionary<string, Func<bool>>
        {
            { "Traitor", () => _sharedRoleSystem.MindTryRemoveRole<TraitorRoleComponent>(mindId) },
            { "Heretic", () => _sharedRoleSystem.MindTryRemoveRole<HereticRoleComponent>(mindId) },
            { "Nukeops", () => _sharedRoleSystem.MindTryRemoveRole<NukeopsRoleComponent>(mindId) },
            { "Revolutionary", () => _sharedRoleSystem.MindTryRemoveRole<RevolutionaryRoleComponent>(mindId) },
            { "Thief", () => _sharedRoleSystem.MindTryRemoveRole<ThiefRoleComponent>(mindId) },
            { "Changeling", () =>
                {
                    var removed = _sharedRoleSystem.MindTryRemoveRole<ChangelingRoleComponent>(mindId);
                    if (_entityManager.TryGetComponent<ChangelingComponent>(antagCharacterEntity, out var changelingComponent))
                    {
                        _entityManager.RemoveComponent<ChangelingComponent>(antagCharacterEntity);
                        shell.WriteLine("ChangelingComponent removed.");
                    }
                    return removed;
                }
            },
            { "InitialInfected", () =>
                {
                    var removed = _sharedRoleSystem.MindTryRemoveRole<InitialInfectedRoleComponent>(mindId);
                    if (_entityManager.TryGetComponent<InitialInfectedComponent>(antagCharacterEntity, out var infectedComponent))
                    {
                        _entityManager.RemoveComponent<InitialInfectedComponent>(antagCharacterEntity);
                        shell.WriteLine("InitialInfectedComponent removed.");
                    }
                    return removed;
                }
            }
        };
        if (roleRemovers.TryGetValue(role, out var remover))
        {
            success = remover.Invoke();
            shell.WriteLine(success ? $"{role} removed successfully!" : $"Failed to remove {role}.");
        }
        else
        {
            shell.WriteError("Unknown or unsupported role.");
        }


        if (success)
            shell.WriteLine($"{role} role successfully removed!");
        else
            shell.WriteLine($"Error while deleting {role} role!");

        // TODO: переписать все роли так, чтобы при удалении компонента у персонажа удалялись и actions
        // TODO: удаление Factions
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
