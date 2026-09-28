using Content.Shared.Actions;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Clothing.Accessories;

public sealed class ADTAccessorySystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private const string UniformSlot = "jumpsuit";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTAccessoryHolderComponent, ComponentInit>(OnHolderInit);
        SubscribeLocalEvent<ADTAccessoryHolderComponent, InteractUsingEvent>(OnHolderInteractUsing);
        SubscribeLocalEvent<ADTAccessoryHolderComponent, GetVerbsEvent<AlternativeVerb>>(OnHolderVerbs);
        SubscribeLocalEvent<ADTAccessoryHolderComponent, ExaminedEvent>(OnHolderExamined);
        SubscribeLocalEvent<ADTAccessoryHolderComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ADTAccessoryHolderComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<ADTAccessoryHolderComponent, GotEquippedEvent>(OnHolderEquipped);
        SubscribeLocalEvent<ADTAccessoryHolderComponent, GotUnequippedEvent>(OnHolderUnequipped);
        SubscribeLocalEvent<ADTAccessoryHolderComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
        SubscribeLocalEvent<ADTAccessoryHolderComponent, InventoryRelayedEvent<CoefficientQueryEvent>>(OnCoefficientQuery);
        SubscribeLocalEvent<ADTAccessoryHolderComponent, EntityTerminatingEvent>(OnHolderTerminating);

        SubscribeLocalEvent<ADTAccessoryComponent, AfterInteractEvent>(OnAccessoryAfterInteract);
        SubscribeLocalEvent<ADTAccessoryComponent, ADTAccessoryAttachDoAfterEvent>(OnAttachDoAfter);
    }

    private void OnHolderInit(Entity<ADTAccessoryHolderComponent> ent, ref ComponentInit args)
    {
        _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerId);
    }

    private void OnHolderInteractUsing(Entity<ADTAccessoryHolderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<ADTAccessoryComponent>(args.Used))
            return;

        args.Handled = true;
        TryAttach(ent, args.Used, args.User);
    }

    private void OnAccessoryAfterInteract(Entity<ADTAccessoryComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!_inventory.TryGetSlotEntity(target, UniformSlot, out var uniform) || !TryComp<ADTAccessoryHolderComponent>(uniform, out var holder))
            return;

        args.Handled = true;

        if (!CanAttach((uniform.Value, holder), ent, args.User))
            return;

        if (target == args.User)
        {
            TryAttach((uniform.Value, holder), ent.Owner, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.AttachDelay, new ADTAccessoryAttachDoAfterEvent(), ent.Owner, target, ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        var message = Loc.GetString("adt-accessory-attach-start",
            ("user", Identity.Entity(args.User, EntityManager)),
            ("accessory", ent.Owner),
            ("target", Identity.Entity(target, EntityManager)));

        _popup.PopupPredicted(message, target, args.User);
    }

    private void OnAttachDoAfter(Entity<ADTAccessoryComponent> ent, ref ADTAccessoryAttachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!_inventory.TryGetSlotEntity(target, UniformSlot, out var uniform) || !TryComp<ADTAccessoryHolderComponent>(uniform, out var holder))
            return;

        TryAttach((uniform.Value, holder), ent.Owner, args.User);
    }

    public bool CanAttach(Entity<ADTAccessoryHolderComponent> holder, Entity<ADTAccessoryComponent> accessory, EntityUid? user)
    {
        var attachedAccessories = GetAccessories(holder);
        if (attachedAccessories.Count >= holder.Comp.MaxAccessories)
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("adt-accessory-full", ("holder", holder.Owner)), holder.Owner, user.Value);

            return false;
        }

        if (accessory.Comp.AllowDuplicates)
            return true;

        var proto = MetaData(accessory.Owner).EntityPrototype?.ID;
        foreach (var attached in attachedAccessories)
        {
            if (MetaData(attached).EntityPrototype?.ID != proto)
                continue;

            if (user != null)
                _popup.PopupClient(Loc.GetString("adt-accessory-duplicate", ("holder", holder.Owner), ("accessory", accessory.Owner)), holder.Owner, user.Value);

            return false;
        }

        return true;
    }

    public bool TryAttach(Entity<ADTAccessoryHolderComponent> holder, Entity<ADTAccessoryComponent?> accessory, EntityUid? user)
    {
        if (!Resolve(accessory.Owner, ref accessory.Comp, false))
            return false;

        if (!CanAttach(holder, (accessory.Owner, accessory.Comp), user))
            return false;

        var container = _container.EnsureContainer<Container>(holder.Owner, holder.Comp.ContainerId);
        if (!_container.Insert(accessory.Owner, container))
            return false;

        if (user != null)
            _popup.PopupClient(Loc.GetString("adt-accessory-attached", ("accessory", accessory.Owner), ("holder", holder.Owner)), holder.Owner, user.Value);

        return true;
    }

    public bool TryDetach(Entity<ADTAccessoryHolderComponent> holder, EntityUid accessory, EntityUid user)
    {
        if (!_container.TryGetContainer(holder.Owner, holder.Comp.ContainerId, out var container)
            || !_container.Remove(accessory, container))
            return false;

        _hands.PickupOrDrop(user, accessory);
        _popup.PopupClient(Loc.GetString("adt-accessory-detached", ("accessory", accessory), ("holder", holder.Owner)), holder.Owner, user);
        return true;
    }

    private void OnHolderVerbs(Entity<ADTAccessoryHolderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        foreach (var accessory in GetAccessories(ent))
        {
            var target = accessory;
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("adt-accessory-verb-detach", ("accessory", target)),
                Priority = -1,
                Act = () => TryDetach(ent, target, user),
            });
        }
    }

    private void OnHolderExamined(Entity<ADTAccessoryHolderComponent> ent, ref ExaminedEvent args)
    {
        var accessories = GetAccessories(ent);
        if (accessories.Count == 0)
            return;

        using (args.PushGroup(nameof(ADTAccessoryHolderComponent)))
        {
            foreach (var accessory in accessories)
            {
                args.PushMarkup(Loc.GetString("adt-accessory-examine", ("accessory", accessory)));
            }
        }
    }

    private void OnInserted(Entity<ADTAccessoryHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        _item.VisualsChanged(ent.Owner);

        if (TryGetWearer(ent.Owner, out var wearer))
            RaiseWornChanged(args.Entity, wearer, ent.Owner, true);
    }

    private void OnRemoved(Entity<ADTAccessoryHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        _item.VisualsChanged(ent.Owner);

        if (TryGetWearer(ent.Owner, out var wearer))
            RaiseWornChanged(args.Entity, wearer, ent.Owner, false);
    }

    private void OnHolderEquipped(Entity<ADTAccessoryHolderComponent> ent, ref GotEquippedEvent args)
    {
        foreach (var accessory in GetAccessories(ent))
        {
            RaiseWornChanged(accessory, args.Equipee, ent.Owner, true);
        }
    }

    private void OnHolderUnequipped(Entity<ADTAccessoryHolderComponent> ent, ref GotUnequippedEvent args)
    {
        foreach (var accessory in GetAccessories(ent))
        {
            RaiseWornChanged(accessory, args.Equipee, ent.Owner, false);
        }
    }

    private void OnDamageModify(Entity<ADTAccessoryHolderComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        foreach (var accessory in GetAccessories(ent))
        {
            if (!TryComp<ArmorComponent>(accessory, out var armor))
                continue;

            args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, armor.Modifiers);
        }
    }

    private void OnCoefficientQuery(Entity<ADTAccessoryHolderComponent> ent, ref InventoryRelayedEvent<CoefficientQueryEvent> args)
    {
        foreach (var accessory in GetAccessories(ent))
        {
            if (!TryComp<ArmorComponent>(accessory, out var armor))
                continue;

            foreach (var (type, value) in armor.Modifiers.Coefficients)
            {
                args.Args.DamageModifiers.Coefficients[type] = args.Args.DamageModifiers.Coefficients.TryGetValue(type, out var coefficient)
                    ? coefficient * value
                    : value;
            }
        }
    }

    private void OnHolderTerminating(Entity<ADTAccessoryHolderComponent> ent, ref EntityTerminatingEvent args)
    {
        if (_net.IsServer && _container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            _container.EmptyContainer(container);
    }

    public IReadOnlyList<EntityUid> GetAccessories(Entity<ADTAccessoryHolderComponent> holder)
    {
        return _container.TryGetContainer(holder.Owner, holder.Comp.ContainerId, out var container)
            ? container.ContainedEntities
            : Array.Empty<EntityUid>();
    }

    private bool TryGetWearer(EntityUid holder, out EntityUid wearer)
    {
        wearer = default;

        if (!_container.TryGetContainingContainer(holder, out var container))
            return false;

        if (!_inventory.TryGetSlotEntity(container.Owner, UniformSlot, out var worn) || worn != holder)
            return false;

        wearer = container.Owner;
        return true;
    }

    private void RaiseWornChanged(EntityUid accessory, EntityUid wearer, EntityUid holder, bool worn)
    {
        UpdateActions(accessory, wearer, worn);

        var ev = new ADTAccessoryWornChangedEvent(wearer, holder, worn);
        RaiseLocalEvent(accessory, ref ev);
    }

    private void UpdateActions(EntityUid accessory, EntityUid wearer, bool worn)
    {
        if (_timing.ApplyingState)
            return;

        if (!worn)
        {
            _actions.RemoveProvidedActions(wearer, accessory);
            return;
        }

        var slots = CompOrNull<ADTAccessoryComponent>(accessory)?.ActionSlots ?? SlotFlags.INNERCLOTHING;
        var ev = new GetItemActionsEvent(_actionContainer, wearer, accessory, slots);
        RaiseLocalEvent(accessory, ev);

        if (ev.Actions.Count == 0)
            return;

        _actions.GrantActions(wearer, ev.Actions, accessory);
    }
}
