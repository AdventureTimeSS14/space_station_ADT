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

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem
{
    private void InitializeSlug()
    {
        SubscribeLocalEvent<ChangelingHeadslugComponent, AfterInteractEvent>(OnLayEggs);
        SubscribeLocalEvent<ChangelingHeadslugComponent, LingEggDoAfterEvent>(OnLayEggsDoAfter);
    }

    private void OnLayEggs(EntityUid uid, ChangelingHeadslugComponent comp, AfterInteractEvent args)
    {
        if (args.Handled)
            return;
        if (!args.Target.HasValue)
            return;

        var target = args.Target.Value;
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

        _container.Insert(uid, containerComp.Container);
    }

    private void UpdateChangelingHeadslug(EntityUid uid, float frameTime, ChangelingHeadslugComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (!comp.IsInside)
            return;

        comp.Accumulator += frameTime;
    }
}
