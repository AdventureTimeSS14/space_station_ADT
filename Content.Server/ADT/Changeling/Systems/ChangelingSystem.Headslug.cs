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

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem
{
    private void InitializeSlug()
    {
        SubscribeLocalEvent<ChangelingHeadslugComponent, LingEggActionEvent>(OnLayEggs);
    }

    private void OnLayEggs(EntityUid uid, ChangelingHeadslugComponent comp, LingEggActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        var containerComp = EnsureComp<ChangelingHeadslugContainerComponent>(target);
        containerComp.Container = _container.EnsureContainer<Container>(target, "headslug", out _);


    }
}
