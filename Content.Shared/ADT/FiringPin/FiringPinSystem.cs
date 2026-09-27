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
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Station;
using Content.Shared.Tag;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
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
    private static readonly VerbCategory SetAlertLevel = new("verb-categories-set-alert-level", null);

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
        SubscribeLocalEvent<FiringPinComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnMapInit(Entity<FiringPinHolderComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient || ent.Comp.StartingPin == null || GetInstalledPin(ent) != null)
            return;

        var container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
        var pin = Spawn(ent.Comp.StartingPin.Value, Transform(ent).Coordinates);
        _container.Insert(pin, container);
    }

    public Entity<FiringPinComponent>? GetInstalledPin(Entity<FiringPinHolderComponent> holder)
    {
        if (!_container.TryGetContainer(holder, holder.Comp.ContainerId, out var container)
            || container is not ContainerSlot slot
            || slot.ContainedEntity is not { Valid: true } contained)
        {
            return null;
        }

        return TryComp<FiringPinComponent>(contained, out var pin) ? (contained, pin) : null;
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
                var container = _container.EnsureContainer<ContainerSlot>(ent, holder.ContainerId);
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

        var pin = GetInstalledPin((ent.Owner, holderComp));
        if (pin == null)
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, holderComp.RemovalDelay,
            new FiringPinRemoveDoAfterEvent { Pin = GetNetEntity(pin.Value.Owner) }, ent.Owner, target: ent.Owner, used: args.Used)
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
        if (pin == null || GetNetEntity(pin.Value.Owner) != args.Pin)
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
        if (IsPinAuthorized(pin.Value, args.User, out var justLinked))
            return;

        if (!justLinked)
            _popup.PopupPredicted(Loc.GetString(pinComp.FailMessage), ent, args.User);

        if (pinComp.SelfDestruct && !justLinked)
        {
            _popup.PopupPredicted(Loc.GetString("firing-pin-selfdestruct"), ent, args.User, PopupType.LargeCaution);

            if (!_net.IsClient)
            {
                _explosion.QueueExplosion(ent.Owner, SharedExplosionSystem.DefaultExplosionPrototypeId,
                    totalIntensity: pinComp.SelfDestructTotalIntensity,
                    slope: pinComp.SelfDestructSlope,
                    maxTileIntensity: pinComp.SelfDestructMaxTileIntensity);
                QueueDel(ent.Owner);
            }
        }

        args.Cancel();
    }

    private bool IsPinAuthorized(Entity<FiringPinComponent> pin, EntityUid user, out bool justLinked)
    {
        justLinked = false;

        if (pin.Comp.Checks.Count == 0)
            return true;

        var hasDnaCheck = false;

        foreach (var check in pin.Comp.Checks)
        {
            if (check.Type == FiringPinType.DNA)
            {
                hasDnaCheck = true;
                continue;
            }

            var authorized = IsCheckAuthorized(pin, user, check);

            if (pin.Comp.Logic == FiringPinLogic.All && !authorized)
                return false;

            if (pin.Comp.Logic == FiringPinLogic.Any && authorized)
                return true;
        }

        if (!hasDnaCheck)
            return pin.Comp.Logic == FiringPinLogic.All;

        var (dnaAuthorized, dnaLinked) = CheckDna(pin, user);
        justLinked = dnaLinked;
        return dnaAuthorized;
    }

    private bool IsCheckAuthorized(Entity<FiringPinComponent> pin, EntityUid user, FiringPinCheck check)
    {
        switch (check.Type)
        {
            case FiringPinType.TestRange:
                return IsNearFiringRange(user);
            case FiringPinType.Implant:
                return HasImplant(user, check.RequiredImplant);
            case FiringPinType.Clown:
                return CheckClown(pin, user, check);
            case FiringPinType.Tag:
                return HasSuit(user, check.RequiredSuitTag);
            case FiringPinType.Access:
                return HasAccess(user, check.RequiredAccess);
            case FiringPinType.SecLevel:
                return IsSecLevelAuthorized(user, check);
            case FiringPinType.Explorer:
                return _station.GetOwningStation(user) == null;
            case FiringPinType.Component:
                if (_whitelist.IsWhitelistPass(check.RequiredWhitelist, user))
                    return true;

                return check.PassForFakeMindShield
                    && TryComp<FakeMindShieldComponent>(user, out var fakeMindShield)
                    && fakeMindShield.IsEnabled;
            case FiringPinType.None:
            default:
                return true;
        }
    }

    private bool IsSecLevelAuthorized(EntityUid user, FiringPinCheck check)
    {
        if (check.AllowedAlertLevels.Count == 0)
            return true;

        if (!TryComp<FiringPinAlertLevelCacheComponent>(user, out var cache))
            return false;

        if (string.IsNullOrEmpty(cache.CurrentLevel))
            return true;

        var currentIndex = check.AllowedAlertLevels.IndexOf(cache.CurrentLevel);
        if (currentIndex < 0)
            return false;

        if (check.SelectedAlertLevel == null)
            return true;

        var selectedIndex = check.AllowedAlertLevels.IndexOf(check.SelectedAlertLevel);
        if (selectedIndex < 0)
            return false;

        return currentIndex <= selectedIndex;
    }

    private void OnGetVerbs(Entity<FiringPinComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var check = GetSecLevelCheck(ent);
        if (check == null || check.AllowedAlertLevels.Count == 0)
            return;

        var user = args.User;

        foreach (var level in check.AllowedAlertLevels)
        {
            var levelCopy = level;

            var verb = new Verb
            {
                Text = Loc.GetString($"alert-level-{levelCopy}"),
                Disabled = check.SelectedAlertLevel == levelCopy,
                Priority = -check.AllowedAlertLevels.IndexOf(levelCopy),
                Category = SetAlertLevel,
                CloseMenu = true,
                Act = () =>
                {
                    check.SelectedAlertLevel = levelCopy;
                    Dirty(ent);

                    _popup.PopupPredicted(Loc.GetString("firing-pin-level-set", ("level", Loc.GetString($"alert-level-{levelCopy}"))), ent, user);
                },
            };

            args.Verbs.Add(verb);
        }
    }

    private FiringPinCheck? GetSecLevelCheck(Entity<FiringPinComponent> ent)
    {
        foreach (var check in ent.Comp.Checks)
        {
            if (check.Type == FiringPinType.SecLevel)
                return check;
        }

        return null;
    }

    private bool IsNearFiringRange(EntityUid user)
    {
        var userPos = _transform.GetMapCoordinates(user);

        var query = EntityQueryEnumerator<FiringRangeComponent, TransformComponent>();
        while (query.MoveNext(out var range, out var comp, out var xform))
        {
            if (userPos.MapId != xform.MapID)
                continue;

            var rangePos = _transform.GetWorldPosition(xform);
            if ((rangePos - userPos.Position).Length() <= comp.Radius)
                return true;
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

    private (bool Authorized, bool JustLinked) CheckDna(Entity<FiringPinComponent> pin, EntityUid user)
    {
        if (pin.Comp.LinkedUser != null)
            return (pin.Comp.LinkedUser == user, false);

        pin.Comp.LinkedUser = user;
        Dirty(pin);

        if (_timing.IsFirstTimePredicted)
            _popup.PopupPredicted(Loc.GetString("firing-pin-dna-locked"), pin.Owner, user);

        return (false, true);
    }

    private bool CheckClown(Entity<FiringPinComponent> pin, EntityUid user, FiringPinCheck check)
    {
        _audio.PlayPredicted(pin.Comp.FailSound, pin.Owner, user);
        return check.PassForClowns && HasComp<ClumsyComponent>(user);
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