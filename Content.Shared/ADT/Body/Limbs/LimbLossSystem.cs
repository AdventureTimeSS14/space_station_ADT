using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Body.Limbs;

public sealed partial class LimbLossSystem : EntitySystem
{
    private const string GlovesSlot = "gloves";
    private const string ShoesSlot = "shoes";
    private const LimbParts BothHands = LimbParts.HandLeft | LimbParts.HandRight;
    private const LimbParts BothFeet = LimbParts.FootLeft | LimbParts.FootRight;

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimbOrganComponent, OrganGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<LimbOrganComponent, OrganGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<LimbLossComponent, IsEquippingTargetAttemptEvent>(OnEquipAttempt);
    }

    private void OnGotInserted(Entity<LimbOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_timing.ApplyingState || !TryComp<LimbLossComponent>(args.Target, out var loss))
            return;

        loss.Missing &= ~ent.Comp.Part;
        Dirty(args.Target, loss);

        SetLayerDisabled(ent, args.Target, false);
    }

    private void OnGotRemoved(Entity<LimbOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (_timing.ApplyingState
            || LifeStage(args.Target) >= EntityLifeStage.Terminating
            || !HasComp<HumanoidProfileComponent>(args.Target))
            return;

        var loss = EnsureComp<LimbLossComponent>(args.Target);
        loss.Missing |= ent.Comp.Part;
        Dirty(args.Target, loss);

        SetLayerDisabled(ent, args.Target, true);

        if ((ent.Comp.Part & LimbParts.Hands) != 0)
        {
            if (AllLost(loss, BothHands))
                _inventory.TryUnequip(args.Target, GlovesSlot, silent: true, force: true);
        }
        else if (AllLost(loss, BothFeet))
        {
            _inventory.TryUnequip(args.Target, ShoesSlot, silent: true, force: true);
        }
    }

    private void OnEquipAttempt(Entity<LimbLossComponent> ent, ref IsEquippingTargetAttemptEvent args)
    {
        if (args.Slot == GlovesSlot && AllLost(ent.Comp, BothHands))
        {
            args.Reason = "limb-loss-cannot-equip-gloves";
            args.Cancel();
        }
        else if (args.Slot == ShoesSlot && AllLost(ent.Comp, BothFeet))
        {
            args.Reason = "limb-loss-cannot-equip-shoes";
            args.Cancel();
        }
    }

    private static bool AllLost(LimbLossComponent loss, LimbParts parts)
    {
        return (loss.Missing & parts) == parts;
    }

    private void SetLayerDisabled(EntityUid organ, EntityUid body, bool disabled)
    {
        if (TryComp<VisualOrganComponent>(organ, out var visual))
            _appearance.SetData(body, visual.Layer, disabled);
    }
}
