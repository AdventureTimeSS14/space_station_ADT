using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Shared.Changeling.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Tag;
using Content.Shared.Roles;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ChangelingRuleSystem : GameRuleSystem<ChangelingRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;

    public readonly ProtoId<NpcFactionPrototype> SyndicateFactionId = "Syndicate";

    public readonly ProtoId<NpcFactionPrototype> NanotrasenFactionId = "NanoTrasen";
    [ValidatePrototypeId<CurrencyPrototype>] public readonly ProtoId<CurrencyPrototype> Currency = "EvolutionPoints";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
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

        _role.MindAddRole(mindId, "MindRoleChangeling", mind, true);

        // briefing
        if (HasComp<MetaDataComponent>(target))
        {
            _antag.SendBriefing(target, Loc.GetString("changeling-role-greeting", ("character-name", Identity.Entity(target, EntityManager))), Color.Red, rule.ChangelingStartSound);
        }

        var store = EnsureComp<StoreComponent>(target);
        foreach (var category in rule.StoreCategories)
            store.Categories.Add(category);
        store.CurrencyWhitelist.Add(Currency);
        store.Balance.Add(Currency, 10);

        var uiComp = EnsureComp<UserInterfaceComponent>(target);
        if (!_userInterfaceSystem.HasUi(target, StoreUiKey.Key, uiComp))
        {
            _userInterfaceSystem.SetUi(target, StoreUiKey.Key, new InterfaceData("StoreBoundUserInterface"));
        }

        _npcFaction.RemoveFaction(target, NanotrasenFactionId, false);
        _npcFaction.AddFaction(target, SyndicateFactionId);

        // Ensure Changeling component and role
        EnsureComp<ChangelingComponent>(target);

        rule.Minds.Add(mindId);

        return true;
    }
}
