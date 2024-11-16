using Content.Shared.Changeling.Components;
using Content.Shared.Changeling;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Content.Shared.IdentityManagement;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Components;
using Content.Server.Destructible;
using Content.Shared.Movement.Systems;
using Content.Shared.ADT.Damage.Events;
using Content.Shared.StatusEffect;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Damage.Components;
using Robust.Shared.Containers;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Server.Resist;
using Content.Shared.Examine;
using System.Linq;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem
{
    private void InitializeSlug()
    {
        SubscribeLocalEvent<ChangelingHeadslugComponent, UserActivateInWorldEvent>(OnLayEggs);
        SubscribeLocalEvent<ChangelingHeadslugComponent, LingEggDoAfterEvent>(OnLayEggsDoAfter);

        SubscribeLocalEvent<ChangelingHeadslugContainerComponent, ExaminedEvent>(OnExamineContainer);
    }

    private void OnLayEggs(EntityUid uid, ChangelingHeadslugComponent comp, UserActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        if (!TryStingTarget(uid, target) || !_mobState.IsDead(target) || HasComp<ChangelingHeadslugContainerComponent>(target))
            return;

        var doAfter = new DoAfterArgs(EntityManager, uid, 4f, new LingEggDoAfterEvent(), uid, target: target)
        {
            DistanceThreshold = 1,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnLayEggsDoAfter(EntityUid uid, ChangelingHeadslugComponent comp, LingEggDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !args.Target.HasValue)
            return;

        var containerComp = EnsureComp<ChangelingHeadslugContainerComponent>(args.Target.Value);
        containerComp.Container = _container.EnsureContainer<Container>(args.Target.Value, "headslug", out _);

        RemComp<CanEscapeInventoryComponent>(uid);
        _container.Insert(uid, containerComp.Container);
        comp.IsInside = true;
        comp.Container = args.Target;
    }

    private void OnExamineContainer(EntityUid uid, ChangelingHeadslugContainerComponent comp, ExaminedEvent args)
    {
        var slug = comp?.Container.ContainedEntities.First();
        if (!slug.HasValue)
            return;

        if (!Comp<ChangelingHeadslugComponent>(slug.Value).Alerted)
            args.PushMarkup(Loc.GetString("changeling-headslug-inside"));
        else
            args.PushMarkup(Loc.GetString("changeling-headslug-inside-soon"));
    }

    private void UpdateChangelingHeadslug(EntityUid uid, float frameTime, ChangelingHeadslugComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (!comp.IsInside || !comp.Container.HasValue)
            return;

        comp.Accumulator += frameTime;

        if (comp.Accumulator >= comp.AccumulateTime * 0.75f && !comp.Alerted)
        {
            _popup.PopupEntity(Loc.GetString("changeling-slug-almost-ready"), uid, uid);
            comp.Alerted = true;
        }

        if (comp.Accumulator < comp.AccumulateTime)
            return;

        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return;

        var monke = Spawn("ADTMobMonkeyChangeling", Transform(comp.Container.Value).Coordinates);
        _mindSystem.TransferTo(mindId, monke);
        var ling = EnsureComp<ChangelingComponent>(monke);

        EntityUid? lesserFormActionEntity = null;
        _action.AddAction(monke, ref lesserFormActionEntity, "ActionLingLesserForm");
        _action.SetToggled(lesserFormActionEntity, true);

        ling.BoughtActions.Add(lesserFormActionEntity);
        ling.LesserFormActive = true;
        ling.LastResortUsed = true;

        _damageableSystem.TryChangeDamage(comp.Container.Value, new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), 500000));
    }
}
