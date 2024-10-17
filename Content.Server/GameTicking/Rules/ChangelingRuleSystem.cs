using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Text;
using Content.Server.Chat.Managers;
using Content.Shared.Changeling.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ChangelingRuleSystem : GameRuleSystem<ChangelingRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;

    public readonly ProtoId<CurrencyPrototype> Currency = "EvolutionPoints";

    public readonly ProtoId<NpcFactionPrototype> SyndicateFactionId = "Syndicate";

    public readonly ProtoId<NpcFactionPrototype> NanotrasenFactionId = "NanoTrasen";
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
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        // briefing
        if (HasComp<MetaDataComponent>(target))
        {
            var briefingShort = Loc.GetString("heretic-role-greeting-short");

            _antag.SendBriefing(target, Loc.GetString("heretic-role-greeting-fluff"), Color.MediumPurple, null);
            _antag.SendBriefing(target, Loc.GetString("heretic-role-greeting"), Color.Red, rule.ChangelingStartSound);

            if (_mind.TryGetRole<RoleBriefingComponent>(mindId, out var rbc))
                rbc.Briefing += $"\n{briefingShort}";
            else _role.MindAddRole(mindId, new RoleBriefingComponent { Briefing = briefingShort }, mind, true);
        }

        _npcFaction.RemoveFaction(target, NanotrasenFactionId, false);
        _npcFaction.AddFaction(target, SyndicateFactionId);

        // Ensure Changeling component and role
        EnsureComp<ChangelingComponent>(target);
        _role.MindAddRole(mindId, new ChangelingRoleComponent(), mind);

        var store = EnsureComp<StoreComponent>(target);
        foreach (var category in rule.StoreCategories)
            store.Categories.Add(category);
        store.CurrencyWhitelist.Add(Currency);
        store.Balance.Add(Currency, 2);

        rule.Minds.Add(mindId);

        foreach (var objective in rule.Objectives)
            _mind.TryAddObjective(mindId, mind, objective);

        return true;
    }


    public void OnObjectivesTextPrepend(Entity<ChangelingRuleComponent> ent, ref ObjectivesTextPrependEvent args)
    {
        var sb = new StringBuilder();

        foreach (var changeling in EntityQuery<ChangelingComponent>())
        {
            if (!_mind.TryGetMind(changeling.Owner, out var mindId, out var mind))
                continue;

            var objectiveText = Loc.GetString("changeling-objective-assignment");
            sb.AppendLine(objectiveText);
        }

        args.Text = sb.ToString();
    }
}
