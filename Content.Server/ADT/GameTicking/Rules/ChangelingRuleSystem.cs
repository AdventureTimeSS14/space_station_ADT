using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using System.Text;
using Content.Shared.Changeling.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Tag;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ChangelingRuleSystem : GameRuleSystem<ChangelingRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public readonly ProtoId<NpcFactionPrototype> SyndicateFactionId = "Syndicate";

    public readonly ProtoId<NpcFactionPrototype> NanotrasenFactionId = "NanoTrasen";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        // SubscribeLocalEvent<ChangelingRuleComponent, ObjectivesTextPrependEvent>(OnObjectivesTextPrepend);
    }

    private void OnAntagSelect(Entity<ChangelingRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        TryMakeChangeling(args.EntityUid, ent.Comp);
    }

    public bool TryMakeChangeling(EntityUid target, ChangelingRuleComponent rule)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;
        if (_tag.HasTag(target, "ChangelingBlacklist"))
            return false;

        // briefing
        if (HasComp<MetaDataComponent>(target))
        {
            _antag.SendBriefing(target, Loc.GetString("changeling-role-greeting", ("character-name", Identity.Entity(target, EntityManager))), Color.Red, rule.ChangelingStartSound);

            _role.MindAddRole(mindId, "MindRoleChangeling", mind, true);
        }

        _npcFaction.RemoveFaction(target, NanotrasenFactionId, false);
        _npcFaction.AddFaction(target, SyndicateFactionId);

        // Ensure Changeling component and role
        EnsureComp<ChangelingComponent>(target);

        rule.Minds.Add(mindId);

        return true;
    }


    // public void OnObjectivesTextPrepend(Entity<ChangelingRuleComponent> ent, ref ObjectivesTextPrependEvent args)
    // {
    //     args.Text += "\n" + Loc.GetString("traitor-round-end-codewords", ("codewords", string.Join(", ", comp.Codewords)));
    // }
}
