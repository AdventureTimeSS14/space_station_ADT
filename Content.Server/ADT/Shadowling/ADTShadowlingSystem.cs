using Content.Shared.ADT.Language;
using Content.Shared.ADT.Shadowling;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Shadowling;

public sealed class ADTShadowlingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedLanguageSystem _language = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    public static readonly ProtoId<LanguagePrototype> HivemindLanguage = "ADTShadowlingCollectiveMind";
    public static readonly ProtoId<NpcFactionPrototype> ShadowlingFaction = "ADTShadowling";
    public static readonly ProtoId<NpcFactionPrototype> NanotrasenFaction = "NanoTrasen";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTShadowlingComponent, MapInitEvent>(OnShadowlingInit);
        SubscribeLocalEvent<ADTShadowlingComponent, ComponentShutdown>(OnShadowlingShutdown);

        SubscribeLocalEvent<ADTShadowlingThrallComponent, MapInitEvent>(OnThrallInit);
        SubscribeLocalEvent<ADTShadowlingThrallComponent, ComponentShutdown>(OnThrallShutdown);
        SubscribeLocalEvent<ADTShadowlingThrallComponent, ExaminedEvent>(OnThrallExamined);

        SubscribeLocalEvent<ADTLesserShadowlingComponent, MapInitEvent>(OnLesserInit);
        SubscribeLocalEvent<ADTLesserShadowlingComponent, ComponentShutdown>(OnLesserShutdown);

        SubscribeLocalEvent<ADTAscendantShadowlingComponent, MapInitEvent>(OnAscendantInit);

        SubscribeLocalEvent<ADTAscendantPhasedComponent, MapInitEvent>(OnPhasedInit);
        SubscribeLocalEvent<ADTAscendantPhasedComponent, ComponentShutdown>(OnPhasedShutdown);
    }

    private void OnShadowlingInit(Entity<ADTShadowlingComponent> ent, ref MapInitEvent args)
    {
        JoinHivemind(ent);

        if (ent.Comp.Hatched)
        {
            GrantActions(ent, ent.Comp.HatchedActions, ent.Comp.GrantedActions);
            RegrantUnlockedAbilities(ent);
        }
        else
        {
            GrantActions(ent, ent.Comp.DisguisedActions, ent.Comp.GrantedActions);
        }
    }

    private void OnShadowlingShutdown(Entity<ADTShadowlingComponent> ent, ref ComponentShutdown args)
    {
        RemoveActions(ent.Comp.GrantedActions);
    }

    public void MarkHatched(Entity<ADTShadowlingComponent> ent)
    {
        if (ent.Comp.Hatched)
            return;

        ent.Comp.Hatched = true;
        Dirty(ent);

        RemoveActions(ent.Comp.GrantedActions);
        GrantActions(ent, ent.Comp.HatchedActions, ent.Comp.GrantedActions);
        RegrantUnlockedAbilities(ent);
    }

    public bool TryUnlockAbility(Entity<ADTShadowlingComponent> ent, EntProtoId action)
    {
        if (!ent.Comp.UnlockedAbilities.Add(action))
            return false;

        GrantActions(ent, new List<EntProtoId> { action }, ent.Comp.GrantedActions);
        return true;
    }

    public void RestoreProgress(Entity<ADTShadowlingComponent> ent)
    {
        RegrantUnlockedAbilities(ent);
    }

    private void RegrantUnlockedAbilities(Entity<ADTShadowlingComponent> ent)
    {
        foreach (var action in ent.Comp.UnlockedAbilities)
        {
            GrantActions(ent, new List<EntProtoId> { action }, ent.Comp.GrantedActions);
        }

        if (ent.Comp.AscensionUnlocked)
            GrantActions(ent, new List<EntProtoId> { ent.Comp.AscendAction }, ent.Comp.GrantedActions);
    }

    public bool TryMakeThrall(EntityUid target, EntityUid? master = null)
    {
        if (HasComp<ADTShadowlingComponent>(target)
            || HasComp<ADTShadowlingThrallComponent>(target)
            || HasComp<ADTAscendantShadowlingComponent>(target))
            return false;

        if (HasComp<MindShieldComponent>(target))
            return false;

        var thrall = EnsureComp<ADTShadowlingThrallComponent>(target);
        thrall.Master = master;
        Dirty(target, thrall);

        var ev = new ADTShadowlingThrallAddedEvent(target, master);
        RaiseLocalEvent(ev);
        return true;
    }

    public bool TryFreeThrall(EntityUid target)
    {
        if (!HasComp<ADTShadowlingThrallComponent>(target))
            return false;

        if (HasComp<ADTLesserShadowlingComponent>(target))
            return false;

        RemComp<ADTShadowlingThrallComponent>(target);
        return true;
    }

    private void OnThrallInit(Entity<ADTShadowlingThrallComponent> ent, ref MapInitEvent args)
    {
        JoinHivemind(ent);

        if (!HasComp<ADTLesserShadowlingComponent>(ent))
            GrantActions(ent, ent.Comp.Actions, ent.Comp.GrantedActions);
    }

    private void OnThrallShutdown(Entity<ADTShadowlingThrallComponent> ent, ref ComponentShutdown args)
    {
        RemoveActions(ent.Comp.GrantedActions);
        LeaveHivemind(ent);
    }

    private void OnThrallExamined(Entity<ADTShadowlingThrallComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (_inventory.TryGetSlotEntity(ent, "mask", out _))
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.ExamineText, ("target", Identity.Entity(ent, EntityManager))));
    }

    private void OnLesserInit(Entity<ADTLesserShadowlingComponent> ent, ref MapInitEvent args)
    {
        JoinHivemind(ent);

        if (TryComp<ADTShadowlingThrallComponent>(ent, out var thrall))
            RemoveActions(thrall.GrantedActions);

        GrantActions(ent, ent.Comp.Actions, ent.Comp.GrantedActions);
    }

    private void OnLesserShutdown(Entity<ADTLesserShadowlingComponent> ent, ref ComponentShutdown args)
    {
        RemoveActions(ent.Comp.GrantedActions);
    }

    private void OnAscendantInit(Entity<ADTAscendantShadowlingComponent> ent, ref MapInitEvent args)
    {
        JoinHivemind(ent);
        GrantActions(ent, ent.Comp.Actions, ent.Comp.GrantedActions);
    }

    private void OnPhasedInit(Entity<ADTAscendantPhasedComponent> ent, ref MapInitEvent args)
    {
        GrantActions(ent, new List<EntProtoId> { ent.Comp.UnphaseAction }, ent.Comp.GrantedActions);
    }

    private void OnPhasedShutdown(Entity<ADTAscendantPhasedComponent> ent, ref ComponentShutdown args)
    {
        RemoveActions(ent.Comp.GrantedActions);
    }

    public void KillOutright(EntityUid target, EntityUid? origin = null)
    {
        if (!_mobThreshold.TryGetDeadThreshold(target, out var threshold))
            return;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Asphyxiation", threshold.Value);
        _damageable.TryChangeDamage(target, damage, true, origin: origin);
    }

    private void GrantActions(EntityUid uid, List<EntProtoId> protos, List<EntityUid> granted)
    {
        foreach (var proto in protos)
        {
            EntityUid? action = null;
            if (!_actions.AddAction(uid, ref action, proto.Id))
                continue;

            if (action != null)
                granted.Add(action.Value);
        }
    }

    private void RemoveActions(List<EntityUid> granted)
    {
        foreach (var action in granted)
        {
            _actions.RemoveAction(action);
        }

        granted.Clear();
    }

    private void JoinHivemind(EntityUid uid)
    {
        _language.AddSpokenLanguage(uid, HivemindLanguage);
        _faction.RemoveFaction(uid, NanotrasenFaction, false);
        _faction.AddFaction(uid, ShadowlingFaction);
    }

    private void LeaveHivemind(EntityUid uid)
    {
        if (HasComp<ADTShadowlingComponent>(uid)
            || HasComp<ADTLesserShadowlingComponent>(uid)
            || HasComp<ADTAscendantShadowlingComponent>(uid))
            return;

        if (TryComp<LanguageSpeakerComponent>(uid, out var speaker))
        {
            speaker.Languages.Remove(HivemindLanguage);
            _language.SelectDefaultLanguage(uid, speaker);
        }

        _faction.RemoveFaction(uid, ShadowlingFaction, false);
        _faction.AddFaction(uid, NanotrasenFaction);
    }

}

public sealed class ADTShadowlingThrallAddedEvent : EntityEventArgs
{
    public readonly EntityUid Thrall;
    public readonly EntityUid? Master;

    public ADTShadowlingThrallAddedEvent(EntityUid thrall, EntityUid? master)
    {
        Thrall = thrall;
        Master = master;
    }
}
