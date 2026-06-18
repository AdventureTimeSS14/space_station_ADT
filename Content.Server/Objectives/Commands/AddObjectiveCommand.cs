using System.Linq;
using Content.Server.ADT.BloodBrothers.Components;
using Content.Server.Administration;
using Content.Shared.ADT.BloodBrothers.Components;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Prototypes;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddObjectiveCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;

    public override string Command => "addobjective";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("cmd-addobjective-invalid-args"));
            return;
        }

        if (!_players.TryGetSessionByUsername(args[0], out var data))
        {
            shell.WriteError(Loc.GetString("cmd-addobjective-player-not-found"));
            return;
        }

        if (!_mind.TryGetMind(data, out var mindId, out var mind))
        {
            shell.WriteError(Loc.GetString("cmd-addobjective-mind-not-found"));
            return;
        }

        if (!_prototypes.TryIndex<EntityPrototype>(args[1], out var proto) ||
            !proto.HasComponent<ObjectiveComponent>())
        {
            shell.WriteError(Loc.GetString("cmd-addobjective-objective-not-found", ("obj", args[1])));
            return;
        }

        if (!_mind.TryAddObjective(mindId, mind, args[1]))
        {
            // can fail for other reasons so dont pretend to be right
            shell.WriteError(Loc.GetString("cmd-addobjective-adding-failed"));
        }

        // ADT-Tweak: also add objective to blood brother partner
        if (_role.MindHasRole<BloodBrotherRoleComponent>(mindId))
        {
            AddObjectiveToBrother(shell, mindId, args[1]);
        }
    }

    private void AddObjectiveToBrother(IConsoleShell shell, EntityUid mindId, string objectiveId)
    {
        var query = _entities.EntityQueryEnumerator<BloodBrotherRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleComp))
        {
            foreach (var team in ruleComp.Teams)
            {
                if (!team.MemberMinds.Contains(mindId))
                    continue;

                foreach (var partnerMind in team.MemberMinds)
                {
                    if (partnerMind == mindId)
                        continue;

                    if (!_entities.TryGetComponent(partnerMind, out MindComponent? partnerMindComp))
                        continue;

                    if (_mind.TryAddObjective(partnerMind, partnerMindComp, objectiveId))
                        shell.WriteLine(Loc.GetString("cmd-addobjective-brother-success"));
                    else
                        shell.WriteError(Loc.GetString("cmd-addobjective-brother-failed"));
                }

                return;
            }
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _players.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();

            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-addobjective-player-completion"));
        }

        if (args.Length != 2)
            return CompletionResult.Empty;

        return CompletionResult.FromHintOptions(
            _objectives.Objectives(),
            Loc.GetString("cmd-add-objective-obj-completion"));
    }
}
