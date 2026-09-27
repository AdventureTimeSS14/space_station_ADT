using System.Linq;
using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Armor;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.ADT.Lavaland;

public sealed class ADTGoliathPlatingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedArmorSystem _armor = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTGoliathPlatableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ADTGoliathPlatableComponent, ADTGoliathPlateDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ADTGoliathPlatableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInteractUsing(Entity<ADTGoliathPlatableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !IsPlatable(ent) || !TryComp<StackComponent>(args.Used, out var stack) || stack.StackTypeId != ent.Comp.Stack)
            return;

        args.Handled = true;

        if (!CanPlate(ent))
        {
            _popup.PopupClient(Loc.GetString("adt-goliath-plating-max"), ent.Owner, args.User);
            return;
        }

        if (ent.Comp.Upgrade != null && _inventory.TryGetContainingSlot(ent.Owner, out _))
        {
            _popup.PopupClient(Loc.GetString("adt-goliath-plating-worn"), ent.Owner, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, new ADTGoliathPlateDoAfterEvent(), ent.Owner, ent.Owner, args.Used)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(Entity<ADTGoliathPlatableComponent> ent, ref ADTGoliathPlateDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used is not { } used || !CanPlate(ent))
            return;

        if (ent.Comp.Upgrade is { } upgrade)
        {
            if (_net.IsClient || _inventory.TryGetContainingSlot(ent.Owner, out _) || !_stack.TryUse(used, 1))
                return;

            args.Handled = true;
            Upgrade(ent, upgrade, args.User);
            return;
        }

        if (!TryComp<ArmorComponent>(ent.Owner, out var armor) || !_stack.TryUse(used, 1))
            return;

        args.Handled = true;

        var modifiers = new DamageModifierSet
        {
            Coefficients = new(armor.Modifiers.Coefficients),
            FlatReduction = new(armor.Modifiers.FlatReduction),
        };

        foreach (var type in ent.Comp.Types)
        {
            var current = modifiers.Coefficients.GetValueOrDefault(type, 1f);
            modifiers.Coefficients[type] = MathF.Max(current - ent.Comp.Step, ent.Comp.MinCoefficient);
        }

        _armor.SetArmorModifiers(ent.Owner, modifiers, armor);
        _popup.PopupClient(Loc.GetString("adt-goliath-plating-done", ("item", ent.Owner)), ent.Owner, args.User);
    }

    private void Upgrade(Entity<ADTGoliathPlatableComponent> ent, string upgrade, EntityUid user)
    {
        var inHand = _hands.IsHolding(user, ent.Owner);
        var coords = _transform.GetMapCoordinates(ent.Owner);
        var upgraded = Spawn(upgrade, coords);

        if (TryComp<ContainerManagerComponent>(ent.Owner, out var oldManager))
        {
            foreach (var container in _container.GetAllContainers(ent.Owner, oldManager))
            {
                if (container.ID == ToggleableClothingComponent.DefaultClothingContainerId)
                    continue;

                if (!_container.TryGetContainer(upgraded, container.ID, out var target))
                    continue;

                foreach (var contained in container.ContainedEntities.ToArray())
                {
                    _container.Insert(contained, target);
                }
            }
        }

        _popup.PopupEntity(Loc.GetString("adt-goliath-plating-done", ("item", upgraded)), upgraded, user);

        if (inHand)
            _hands.TryDrop(user, ent.Owner, checkActionBlocker: false, doDropInteraction: false);

        QueueDel(ent.Owner);

        if (inHand)
            _hands.PickupOrDrop(user, upgraded);
    }

    private void OnExamined(Entity<ADTGoliathPlatableComponent> ent, ref ExaminedEvent args)
    {
        if (!IsPlatable(ent))
            return;

        var message = CanPlate(ent)
            ? "adt-goliath-plating-examine"
            : "adt-goliath-plating-examine-max";

        args.PushMarkup(Loc.GetString(message));
    }

    private bool IsPlatable(Entity<ADTGoliathPlatableComponent> ent)
    {
        return ent.Comp.Upgrade != null || ent.Comp.Types.Count > 0;
    }

    private bool CanPlate(Entity<ADTGoliathPlatableComponent> ent)
    {
        if (ent.Comp.Upgrade != null)
            return true;

        if (!TryComp<ArmorComponent>(ent.Owner, out var armor))
            return false;

        foreach (var type in ent.Comp.Types)
        {
            if (armor.Modifiers.Coefficients.GetValueOrDefault(type, 1f) > ent.Comp.MinCoefficient)
                return true;
        }

        return false;
    }
}
