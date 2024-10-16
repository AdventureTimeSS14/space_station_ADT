using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Text;
using System.Linq;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Objectives;
using Content.Shared.IdentityManagement;
using Content.Server.Roles;
using Content.Server.Shuttles.Components;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Actions;
using Content.Shared.NPC.Systems;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ChangelingRuleSystem : GameRuleSystem<ChangelingRuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;

    private int PlayersPerLing => _cfg.GetCVar(CCVars.ChangelingPlayersPerChangeling);
    private int MaxChangelings => _cfg.GetCVar(CCVars.ChangelingMaxChangelings);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        SubscribeLocalEvent<ChangelingRuleComponent, ObjectivesTextPrependEvent>(OnObjectivesTextPrepend);
    }

    private void OnAntagSelect(Entity<ChangelingRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        TryMakeChangeling(args.EntityUid, ent.Comp);

        for (int i = 0; i < _random.Next(6, 12); i++)
            if (TryFindRandomTile(out var _, out var _, out var _, out var coords))
                Spawn("ChangelingInfluence", coords);
    }

    public bool TryMakeChangeling(EntityUid target, ChangelingRuleComponent rule)
    {
        if (!_mindSystem.TryGetMind(target, out var mindId, out var mind))
            return false;

        _antagSelection.SendBriefing(target, Loc.GetString("changeling-role-greeting"), Color.Purple, rule.ChangelingStartSound);

        _npcFaction.RemoveFaction(target, "NanoTrasen", false);
        _npcFaction.AddFaction(target, "Syndicate");

        // Ensure Changeling component and role
        EnsureComp<ChangelingComponent>(target);
        _roleSystem.MindAddRole(mindId, new ChangelingRoleComponent(), mind);

        // Assign objectives and other components
        rule.Minds.Add(mindId);

        foreach (var objective in rule.Objectives)
            _mindSystem.TryAddObjective(mindId, mind, objective);

        return true;
    }

    public void OnObjectivesTextPrepend(Entity<ChangelingRuleComponent> ent, ref ObjectivesTextPrependEvent args)
    {
        var sb = new StringBuilder();
        foreach (var changeling in EntityQuery<ChangelingComponent>())
        {
            if (!_mindSystem.TryGetMind(changeling.Owner, out var mindId, out var mind))
                continue;

            var objectiveText = Loc.GetString("changeling-objective-assignment");
            sb.AppendLine(objectiveText);
        }

        args.Text = sb.ToString();
    }
}
