using Content.Shared.ADT.Janicart;
using Content.Shared.ADT.Janicart.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Fluids.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Tools.Systems;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Janicart;

public sealed class ADTJanicartSystem : SharedADTJanicartSystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    private readonly HashSet<Entity<PuddleComponent>> _puddles = [];
    private readonly HashSet<Entity<ItemComponent>> _items = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTJanicartUpgradeableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ADTJanicartUpgradeableComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<ADTJanicartUpgradeableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ADTJanicartUpgradeableComponent, ADTJanicartUpgradeRemoveDoAfterEvent>(OnRemoveDoAfter);
        SubscribeLocalEvent<ADTJanicartUpgradeableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ADTJanicartUpgradeableComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);

        SubscribeLocalEvent<ADTJanicartUpgradeComponent, ExaminedEvent>(OnUpgradeExamined);

        SubscribeLocalEvent<BorgModuleComponent, BorgModuleInstalledEvent>(OnBorgModuleInstalled);
        SubscribeLocalEvent<BorgModuleComponent, BorgModuleUninstalledEvent>(OnBorgModuleUninstalled);
    }

    private void OnInit(Entity<ADTJanicartUpgradeableComponent> ent, ref ComponentInit args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.UpgradesContainerId);
    }

    private void OnAfterInteractUsing(Entity<ADTJanicartUpgradeableComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        args.Handled = TryInstall(ent, args.Used, args.User);
    }

    private bool TryInstall(Entity<ADTJanicartUpgradeableComponent> ent, EntityUid used, EntityUid user)
    {
        if (!TryComp<ADTJanicartUpgradeComponent>(used, out var upgrade))
            return false;

        if (IsTypeLimitReached(ent, (used, upgrade)))
        {
            _popup.PopupClient(Loc.GetString("janicart-upgrade-popup-conflict"), ent, user);
            return false;
        }

        if (!_container.Insert(used, _container.GetContainer(ent, ent.Comp.UpgradesContainerId)))
            return false;

        _audio.PlayPredicted(ent.Comp.InsertSound, ent, user);
        _popup.PopupClient(Loc.GetString("janicart-upgrade-popup-insert", ("upgrade", used), ("vehicle", ent.Owner)), user);
        OnUpgradesChanged(ent);
        AddActiveModule(ent, used);

        _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user):player} installed {ToPrettyString(used)} into {ToPrettyString(ent.Owner)}.");
        return true;
    }

    private bool IsTypeLimitReached(Entity<ADTJanicartUpgradeableComponent> ent, Entity<ADTJanicartUpgradeComponent> upgrade)
    {
        if (upgrade.Comp.MaximumOfType <= 0)
            return false;

        var sameType = 0;
        foreach (var installed in GetUpgrades(ent))
        {
            foreach (var tag in installed.Comp.Tags)
            {
                if (!upgrade.Comp.Tags.Contains(tag))
                    continue;

                sameType++;
                break;
            }
        }

        return sameType >= upgrade.Comp.MaximumOfType;
    }

    private void OnInteractUsing(Entity<ADTJanicartUpgradeableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, ent.Comp.RemoveTool))
            return;

        if (GetUpgrades(ent).Count == 0)
        {
            _popup.PopupClient(Loc.GetString("janicart-upgrade-popup-nothing-installed"), ent, args.User);
            args.Handled = true;
            return;
        }

        args.Handled = _tool.UseTool(args.Used,
            args.User,
            ent,
            ent.Comp.RemoveDelay,
            new[] { ent.Comp.RemoveTool },
            new ADTJanicartUpgradeRemoveDoAfterEvent());
    }

    private void OnRemoveDoAfter(Entity<ADTJanicartUpgradeableComponent> ent, ref ADTJanicartUpgradeRemoveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        RemoveAllUpgrades(ent, args.User);
    }

    private void RemoveAllUpgrades(Entity<ADTJanicartUpgradeableComponent> ent, EntityUid? user = null)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.UpgradesContainerId, out var container))
            return;

        foreach (var upgrade in GetUpgrades(ent))
            RemoveActiveModule(ent, upgrade.Owner);

        _container.EmptyContainer(container);
        _audio.PlayPredicted(ent.Comp.RemoveSound, ent, user);

        if (user != null)
        {
            _popup.PopupClient(Loc.GetString("janicart-upgrade-popup-removed", ("vehicle", ent.Owner)), ent, user.Value);
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user.Value):player} removed upgrades from {ToPrettyString(ent.Owner)}.");
        }

        OnUpgradesChanged(ent);
    }

    private void OnUpgradesChanged(Entity<ADTJanicartUpgradeableComponent> ent)
    {
        var buffer = false;
        foreach (var upgrade in GetUpgrades(ent))
        {
            if (HasComp<ADTJanicartBufferComponent>(upgrade))
            {
                buffer = true;
                break;
            }
        }

        _appearance.SetData(ent, ADTJanicartUpgradeVisuals.Buffer, buffer);
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnRefreshSpeed(Entity<ADTJanicartUpgradeableComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        foreach (var upgrade in GetUpgrades(ent))
        {
            if (TryComp<ADTJanicartSpeedComponent>(upgrade, out var speed))
            {
                args.ModifySpeed(speed.SpeedMultiplier);
                break;
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        List<EntityUid>? hostsToRemove = null;
        var query = EntityQueryEnumerator<ADTJanicartActiveModulesComponent>();

        while (query.MoveNext(out var host, out var active))
        {
            List<EntityUid>? toRemove = null;

            foreach (var moduleUid in active.ActiveModules)
            {
                if (!Exists(moduleUid))
                {
                    (toRemove ??= []).Add(moduleUid);
                    continue;
                }

                if (!IsModuleOperated(moduleUid))
                    continue;

                if (TryComp<ADTJanicartBufferComponent>(moduleUid, out var buffer)
                    && curTime >= buffer.NextCheck)
                {
                    buffer.NextCheck = curTime + buffer.CheckInterval;
                    WashPuddles(moduleUid, buffer);
                }

                if (TryComp<ADTJanicartVacuumComponent>(moduleUid, out var vacuum)
                    && curTime >= vacuum.NextCheck)
                {
                    vacuum.NextCheck = curTime + vacuum.CheckInterval;
                    CollectTrash(moduleUid, vacuum);
                }
            }

            if (toRemove != null)
            {
                foreach (var uid in toRemove)
                    active.ActiveModules.Remove(uid);
            }

            if (active.ActiveModules.Count == 0)
                (hostsToRemove ??= []).Add(host);
        }

        if (hostsToRemove != null)
        {
            foreach (var host in hostsToRemove)
                RemComp<ADTJanicartActiveModulesComponent>(host);
        }
    }

    private void AddActiveModule(EntityUid host, EntityUid moduleUid)
    {
        if (!HasComp<ADTJanicartBufferComponent>(moduleUid) && !HasComp<ADTJanicartVacuumComponent>(moduleUid))
            return;

        var active = EnsureComp<ADTJanicartActiveModulesComponent>(host);
        active.ActiveModules.Add(moduleUid);
    }

    private void RemoveActiveModule(EntityUid host, EntityUid moduleUid)
    {
        if (!TryComp<ADTJanicartActiveModulesComponent>(host, out var active))
            return;

        active.ActiveModules.Remove(moduleUid);
        if (active.ActiveModules.Count == 0)
            RemComp<ADTJanicartActiveModulesComponent>(host);
    }

    private bool IsModuleOperated(EntityUid moduleUid)
    {
        var host = Transform(moduleUid).ParentUid;
        if (!Exists(host))
            return false;

        if (TryComp<VehicleComponent>(host, out var vehicle))
            return vehicle.Operator != null;

        if (HasComp<BorgChassisComponent>(host))
            return _mind.TryGetMind(host, out _, out _);

        return false;
    }

    private void OnBorgModuleInstalled(Entity<BorgModuleComponent> ent, ref BorgModuleInstalledEvent args)
    {
        if (_timing.ApplyingState)
            return;

        AddActiveModule(args.ChassisEnt, ent);
    }

    private void OnBorgModuleUninstalled(Entity<BorgModuleComponent> ent, ref BorgModuleUninstalledEvent args)
    {
        if (_timing.ApplyingState)
            return;

        RemoveActiveModule(args.ChassisEnt, ent);
    }

    private void WashPuddles(EntityUid moduleUid, ADTJanicartBufferComponent buffer)
    {
        _puddles.Clear();
        _lookup.GetEntitiesInRange(Transform(moduleUid).Coordinates, buffer.Range, _puddles);

        var cleaned = false;
        foreach (var puddle in _puddles)
        {
            SpawnAttachedTo(buffer.MoppedEffect, Transform(puddle).Coordinates);
            QueueDel(puddle);
            cleaned = true;
        }

        if (cleaned)
            _audio.PlayPvs(buffer.WashSound, moduleUid);
    }

    private void CollectTrash(EntityUid moduleUid, ADTJanicartVacuumComponent vacuum)
    {
        var owner = Transform(moduleUid).ParentUid;
        if (!TryGetTrashBag(owner, vacuum, out var bag, out var storage))
            return;

        _items.Clear();
        _lookup.GetEntitiesInRange<ItemComponent>(Transform(owner).Coordinates, vacuum.Range, _items);

        var collected = 0;
        foreach (var item in _items)
        {
            if (item.Owner == owner
                || _container.IsEntityInContainer(item)
                || !_tag.HasTag(item.Owner, vacuum.TrashTag))
            {
                continue;
            }

            if (!_storage.CanInsert(bag, item.Owner, out _, storageComp: storage)
                || !_storage.Insert(bag, item.Owner, out _, storageComp: storage))
            {
                continue;
            }

            if (++collected >= vacuum.MaxItemsPerCheck)
                break;
        }

        if (collected > 0)
            _audio.PlayPvs(vacuum.CollectSound, owner);
    }

    private bool TryGetTrashBag(EntityUid uid, ADTJanicartVacuumComponent vacuum, out EntityUid bag, out StorageComponent storage)
    {
        bag = EntityUid.Invalid;
        storage = default!;

        if (vacuum.TrashBagSlot != string.Empty)
        {
            var bagUid = _slots.GetItemOrNull(uid, vacuum.TrashBagSlot);
            if (bagUid is { } inSlot && TryComp<StorageComponent>(inSlot, out var slotStorage))
            {
                bag = inSlot;
                storage = slotStorage;
                return true;
            }

            return false;
        }

        if (!TryComp<HandsComponent>(uid, out var hands))
            return false;

        foreach (var handId in _hands.EnumerateHands(uid))
        {
            if (!_hands.TryGetHeldItem(uid, handId, out var held) || held == uid)
                continue;

            if (_tag.HasTag(held.Value, vacuum.TrashBagTag) && TryComp<StorageComponent>(held.Value, out var handStorage))
            {
                bag = held.Value;
                storage = handStorage;
                return true;
            }
        }

        return false;
    }

    private void OnExamined(Entity<ADTJanicartUpgradeableComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(ADTJanicartUpgradeableComponent)))
        {
            var upgrades = GetUpgrades(ent);
            if (upgrades.Count == 0)
            {
                args.PushMarkup(Loc.GetString("janicart-upgrade-examine-none"));
                return;
            }

            args.PushMarkup(Loc.GetString("janicart-upgrade-examine-installed"));
            foreach (var upgrade in upgrades)
            {
                args.PushMarkup(Loc.GetString("janicart-upgrade-examine-entry", ("upgrade", upgrade.Owner)));
            }
        }
    }

    private void OnUpgradeExamined(Entity<ADTJanicartUpgradeComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExamineText != string.Empty)
            args.PushMarkup(Loc.GetString(ent.Comp.ExamineText));
    }

    private HashSet<Entity<ADTJanicartUpgradeComponent>> GetUpgrades(Entity<ADTJanicartUpgradeableComponent> ent)
    {
        var upgrades = new HashSet<Entity<ADTJanicartUpgradeComponent>>();
        if (!_container.TryGetContainer(ent, ent.Comp.UpgradesContainerId, out var container))
            return upgrades;

        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp<ADTJanicartUpgradeComponent>(contained, out var upgrade))
                upgrades.Add((contained, upgrade));
        }

        return upgrades;
    }
}