using System.Numerics;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.Clumsy;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Station;
using Content.Shared.Tag;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.FiringPin;

public sealed partial class FiringPinSystem : EntitySystem
{
    private const float TestRangeRadius = 10f;

    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FiringPinHolderComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GunComponent, InteractUsingEvent>(OnInteractUsing, before: new[] { typeof(ItemSlotsSystem) });
        SubscribeLocalEvent<FiringPinHolderComponent, FiringPinRemoveDoAfterEvent>(OnRemoveDoAfter);
        SubscribeLocalEvent<GunComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<FiringPinHolderComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<GunComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnMapInit(Entity<FiringPinHolderComponent> ent, ref MapInitEvent args)
    {
        var container = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);

        if (ent.Comp.StartingPin == null || GetInstalledPin(ent) != null || _net.IsClient)
            return;

        var pin = Spawn(ent.Comp.StartingPin.Value, Transform(ent).Coordinates);
        _container.Insert(pin, container);
    }

    public Entity<FiringPinComponent>? GetInstalledPin(Entity<FiringPinHolderComponent> holder)
    {
        if (!_container.TryGetContainer(holder, holder.Comp.ContainerId, out var container))
            return null;

        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp<FiringPinComponent>(contained, out var pin))
                return (contained, pin);
        }

        return null;
    }

    private void OnInteractUsing(Entity<GunComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<FiringPinComponent>(args.Used, out var pinComp))
        {
            var holder = EnsureComp<FiringPinHolderComponent>(ent.Owner);

            if (_whitelist.IsWhitelistPassOrNull(holder.Whitelist, args.Used))
            {
                var container = _container.EnsureContainer<Container>(ent, holder.ContainerId);
                var existing = GetInstalledPin((ent.Owner, holder));

                if (existing != null)
                {
                    if (!pinComp.ForceReplace)
                    {
                        _popup.PopupPredicted(Loc.GetString("firing-pin-replace-denied"), ent, args.User);
                        args.Handled = true;
                        return;
                    }

                    _container.Remove(existing.Value.Owner, container);
                    _hands.TryPickupAnyHand(args.User, existing.Value.Owner);
                }

                if (_container.Insert(args.Used, container))
                {
                    _audio.PlayPredicted(holder.InsertSound, ent, args.User);
                    _popup.PopupPredicted(Loc.GetString("firing-pin-inserted", ("pin", args.Used), ("gun", ent.Owner)), ent, args.User);
                }
            }

            args.Handled = true;
            return;
        }

        if (!TryComp<FiringPinHolderComponent>(ent, out var holderComp) || !HasComp<ToolComponent>(args.Used))
            return;

        if (GetInstalledPin((ent.Owner, holderComp)) == null)
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, holderComp.RemovalDelay,
            new FiringPinRemoveDoAfterEvent(), ent.Owner, target: ent.Owner, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f,
        });

        args.Handled = true;
    }

    private void OnRemoveDoAfter(Entity<FiringPinHolderComponent> ent, ref FiringPinRemoveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var pin = GetInstalledPin(ent);
        if (pin == null)
            return;

        _audio.PlayPredicted(ent.Comp.RemoveSound, ent, args.User);
        _popup.PopupPredicted(Loc.GetString("firing-pin-removed"), ent, args.User);

        if (!_net.IsClient)
            QueueDel(pin.Value.Owner);

        args.Handled = true;
    }

    private void OnShotAttempted(Entity<GunComponent> ent, ref ShotAttemptedEvent args)
    {
        if (!TryComp<FiringPinHolderComponent>(ent, out var holder))
            return;

        if (holder.Emagged)
            return;

        var pin = GetInstalledPin((ent.Owner, holder));

        if (pin == null)
        {
            if (holder.RequiresPin)
            {
                _popup.PopupPredicted(Loc.GetString("firing-pin-no-pin"), ent, args.User);
                args.Cancel();
            }
            return;
        }

        var pinComp = pin.Value.Comp;
        if (IsPinAuthorized(pin.Value, args.User))
            return;

        var justLinked = pinComp.PinType == FiringPinType.DNA
            && pinComp.LinkedUser == args.User;

        if (!justLinked)
            _popup.PopupPredicted(Loc.GetString(pinComp.FailMessage), ent, args.User);

        if (pinComp.SelfDestruct && !justLinked)
        {
            _popup.PopupPredicted(Loc.GetString("firing-pin-selfdestruct"), ent, args.User, PopupType.LargeCaution);

            if (!_net.IsClient)
            {
                _explosion.QueueExplosion(ent.Owner, SharedExplosionSystem.DefaultExplosionPrototypeId,
                    totalIntensity: 2f, slope: 5f, maxTileIntensity: 2f);
                QueueDel(ent.Owner);
            }
        }

        args.Cancel();
    }

    private bool IsPinAuthorized(Entity<FiringPinComponent> pin, EntityUid user)
    {
        switch (pin.Comp.PinType)
        {
            case FiringPinType.TestRange:
                return IsNearFiringRange(user);
            case FiringPinType.Implant:
                return HasImplant(user, pin.Comp.RequiredImplant);
            case FiringPinType.DNA:
                return CheckDna(pin, user);
            case FiringPinType.Clown:
                return CheckClown(pin, user);
            case FiringPinType.Tag:
                return HasSuit(user, pin.Comp.RequiredSuitTag);
            case FiringPinType.Access:
                return HasAccess(user, pin.Comp.RequiredAccess);
            case FiringPinType.SecLevel:
                return IsSecLevelAuthorized(user, pin.Comp);
            case FiringPinType.Explorer:
                return _station.GetOwningStation(user) == null;
            case FiringPinType.Component:
                return _whitelist.IsWhitelistPass(pin.Comp.RequiredWhitelist, user);
            case FiringPinType.None:
            default:
                return true;
        }
    }

    private bool IsSecLevelAuthorized(EntityUid user, FiringPinComponent pin)
    {
        if (pin.AllowedAlertLevels.Count == 0)
            return true;

        if (!TryComp<FiringPinAlertLevelCacheComponent>(user, out var cache)
            || string.IsNullOrEmpty(cache.CurrentLevel))
        {
            return true;
        }

        return pin.AllowedAlertLevels.Contains(cache.CurrentLevel);
    }

    private bool IsNearFiringRange(EntityUid user)
    {
        var userPos = _transform.GetMapCoordinates(user);

        var query = EntityQueryEnumerator<FiringRangeComponent>();
        while (query.MoveNext(out var range, out _))
        {
            var rangePos = _transform.GetMapCoordinates(range);
            if (userPos.MapId == rangePos.MapId
                && (rangePos.Position - userPos.Position).Length() <= TestRangeRadius)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasImplant(EntityUid user, EntProtoId? requiredImplant)
    {
        if (requiredImplant == null)
            return true;

        if (!TryComp<ImplantedComponent>(user, out var implanted))
            return false;

        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            if (MetaData(implant).EntityPrototype?.ID == requiredImplant.Value.Id)
                return true;
        }

        return false;
    }

    private bool CheckDna(Entity<FiringPinComponent> pin, EntityUid user)
    {
        if (pin.Comp.LinkedUser != null)
            return pin.Comp.LinkedUser == user;

        pin.Comp.LinkedUser = user;
        Dirty(pin);

        if (_timing.IsFirstTimePredicted)
            _popup.PopupPredicted(Loc.GetString("firing-pin-dna-locked"), pin.Owner, user);

        return false;
    }

    private bool CheckClown(Entity<FiringPinComponent> pin, EntityUid user)
    {
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/bikehorn.ogg"), pin.Owner, user);
        return pin.Comp.PassForClowns && HasComp<ClumsyComponent>(user);
    }

    private bool HasSuit(EntityUid user, ProtoId<TagPrototype>? requiredTag)
    {
        if (requiredTag == null)
            return true;

        if (!_inventory.TryGetSlotEntity(user, "outerClothing", out var suit))
            return false;

        return _tag.HasTag(suit.Value, requiredTag.Value);
    }

    private bool HasAccess(EntityUid user, List<ProtoId<AccessLevelPrototype>> required)
    {
        if (required.Count == 0)
            return true;

        var tags = _accessReader.FindAccessTags(user);

        foreach (var access in required)
        {
            if (tags.Contains(access))
                return true;
        }

        return false;
    }

    private void OnExamine(Entity<FiringPinHolderComponent> ent, ref ExaminedEvent args)
    {
        var pin = GetInstalledPin(ent);

        if (pin != null)
            args.PushMarkup(Loc.GetString("firing-pin-examine-installed", ("pin", pin.Value)));
        else if (ent.Comp.RequiresPin)
            args.PushMarkup(Loc.GetString("firing-pin-examine-missing"));
    }

    private void OnEmagged(Entity<GunComponent> ent, ref GotEmaggedEvent args)
    {
        if (!TryComp<FiringPinHolderComponent>(ent, out var holder))
            return;

        if (holder.Emagged)
            return;

        holder.Emagged = true;
        Dirty(ent.Owner, holder);
        args.Handled = true;
        args.Repeatable = true;
    }
}