using Content.Shared.ADT.Weapons.KineticCooldown;
using Content.Shared.ADT.Weapons.Ranged.Upgrades.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Weapons.Ranged.Upgrades;

public sealed class ADTGunUpgradeSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    private const float MinUpgradeMultiplier = 0.4f;

    public override void Initialize()
    {
        SubscribeLocalEvent<ADTUpgradeableGunComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ADTUpgradeableGunComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<ADTUpgradeableGunComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ADTUpgradeableGunComponent, ADTGunUpgradeRemoveDoAfterEvent>(OnRemoveDoAfter);
        SubscribeLocalEvent<ADTUpgradeableGunComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<ADTUpgradeableGunComponent, GunRefreshModifiersEvent>(RelayEvent);
        SubscribeLocalEvent<ADTUpgradeableGunComponent, AmmoShotEvent>(OnAmmoShot);

        SubscribeLocalEvent<ADTGunUpgradeComponent, ExaminedEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<ADTGunUpgradeFireRateComponent, GunRefreshModifiersEvent>(OnFireRateRefresh);
        SubscribeLocalEvent<ADTGunUpgradeRepeaterComponent, GunRefreshModifiersEvent>(OnRepeaterRefresh);

        Subs.BuiEvents<ADTGunUpgradeTracerComponent>(ADTGunUpgradeTracerUiKey.Key, subs =>
        {
            subs.Event<ADTGunUpgradeTracerColorMessage>(OnTracerColorPicked);
        });
    }

    private void OnInit(Entity<ADTUpgradeableGunComponent> ent, ref ComponentInit args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.UpgradesContainerId);
    }

    private void OnAfterInteractUsing(Entity<ADTUpgradeableGunComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        args.Handled = TryInstall(ent, args.Used, args.User);
    }

    public bool TryInstall(Entity<ADTUpgradeableGunComponent> ent, EntityUid used, EntityUid user)
    {
        if (!TryComp<ADTGunUpgradeComponent>(used, out var upgrade))
            return false;

        if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, used))
            return false;

        var remaining = GetRemainingCapacity(ent);
        if (remaining < upgrade.Cost)
        {
            _popup.PopupClient(Loc.GetString("adt-gun-upgrade-popup-no-room", ("remaining", remaining), ("cost", upgrade.Cost)), ent, user);
            return false;
        }

        if (IsTypeLimitReached(ent, (used, upgrade)))
        {
            _popup.PopupClient(Loc.GetString("adt-gun-upgrade-popup-conflict"), ent, user);
            return false;
        }

        if (!_container.Insert(used, _container.GetContainer(ent, ent.Comp.UpgradesContainerId)))
            return false;

        _audio.PlayPredicted(ent.Comp.InsertSound, ent, user);
        _popup.PopupClient(Loc.GetString("gun-upgrade-popup-insert", ("upgrade", used), ("gun", ent.Owner)), user);
        OnUpgradesChanged(ent);

        _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user):player} inserted gun upgrade {ToPrettyString(used)} into {ToPrettyString(ent.Owner)}.");
        return true;
    }

    private bool IsTypeLimitReached(Entity<ADTUpgradeableGunComponent> ent, Entity<ADTGunUpgradeComponent> upgrade)
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

    private void OnInteractUsing(Entity<ADTUpgradeableGunComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, ent.Comp.RemoveTool))
            return;

        if (GetUpgrades(ent).Count == 0)
        {
            _popup.PopupClient(Loc.GetString("adt-gun-upgrade-popup-nothing-installed"), ent, args.User);
            args.Handled = true;
            return;
        }

        args.Handled = _tool.UseTool(args.Used,
            args.User,
            ent,
            ent.Comp.RemoveDelay,
            new[] { ent.Comp.RemoveTool.Id },
            new ADTGunUpgradeRemoveDoAfterEvent());
    }

    private void OnRemoveDoAfter(Entity<ADTUpgradeableGunComponent> ent, ref ADTGunUpgradeRemoveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        RemoveAllUpgrades(ent, args.User);
    }

    public void RemoveAllUpgrades(Entity<ADTUpgradeableGunComponent> ent, EntityUid? user = null)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.UpgradesContainerId, out var container))
            return;

        _container.EmptyContainer(container);
        _audio.PlayPredicted(ent.Comp.RemoveSound, ent, user);

        if (user != null)
            _popup.PopupClient(Loc.GetString("adt-gun-upgrade-popup-removed", ("gun", ent.Owner)), ent, user.Value);

        OnUpgradesChanged(ent);
    }

    private void OnUpgradesChanged(Entity<ADTUpgradeableGunComponent> ent)
    {
        _gun.RefreshModifiers(ent.Owner);
        RefreshRecharge(ent);

        if (!_net.IsServer)
            return;

        RefreshChassis(ent);
    }

    private void RefreshRecharge(Entity<ADTUpgradeableGunComponent> ent)
    {
        if (!TryComp<ADTKineticCooldownComponent>(ent, out var cooldown))
            return;

        var rangedModifier = 1f;
        var meleeModifier = 1f;

        foreach (var upgrade in GetUpgrades(ent))
        {
            if (TryComp<ADTGunUpgradeFireRateComponent>(upgrade, out var fireRate))
                rangedModifier *= fireRate.RechargeCoefficient;

            if (TryComp<ADTGunUpgradeRepeaterComponent>(upgrade, out var repeater))
            {
                rangedModifier *= repeater.RechargeCoefficient;
                meleeModifier *= repeater.RechargeCoefficient;
            }
        }

        cooldown.UpgradeRangedMultiplier = MathF.Max(rangedModifier, MinUpgradeMultiplier);
        cooldown.UpgradeMeleeMultiplier = MathF.Max(meleeModifier, MinUpgradeMultiplier);
        Dirty(ent, cooldown);
    }

    private void RefreshChassis(Entity<ADTUpgradeableGunComponent> ent)
    {
        ADTGunUpgradeChassisComponent? chassis = null;
        foreach (var upgrade in GetUpgrades(ent))
        {
            if (TryComp(upgrade, out chassis))
                break;
        }

        if (chassis == null)
        {
            if (!TryComp<ADTChassisPaintedComponent>(ent, out var painted))
                return;

            _metaData.SetEntityName(ent, painted.OriginalName ?? Name(ent));
            _appearance.SetData(ent, ADTGunChassisVisuals.Chassis, string.Empty);
            _appearance.SetData(ent, ADTGunChassisVisuals.Color, Color.White);
            RemComp<ADTChassisPaintedComponent>(ent);
            return;
        }

        var comp = EnsureComp<ADTChassisPaintedComponent>(ent);
        comp.OriginalName ??= Name(ent);

        if (chassis.ChassisName != string.Empty)
            _metaData.SetEntityName(ent, Loc.GetString(chassis.ChassisName));

        var hasSprite = ent.Comp.ChassisSprites.ContainsKey(chassis.ChassisId);
        _appearance.SetData(ent, ADTGunChassisVisuals.Chassis, hasSprite ? chassis.ChassisId : string.Empty);
        _appearance.SetData(ent, ADTGunChassisVisuals.Color, hasSprite ? Color.White : chassis.ChassisColor);
    }

    private void OnFireRateRefresh(Entity<ADTGunUpgradeFireRateComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (HasComp<ADTKineticCooldownComponent>(args.Gun))
            return;

        args.FireRate *= ent.Comp.Coefficient;
    }

    private void OnRepeaterRefresh(Entity<ADTGunUpgradeRepeaterComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (HasComp<ADTKineticCooldownComponent>(args.Gun))
            return;

        args.FireRate *= ent.Comp.FireRateCoefficient;
    }

    private void OnTracerColorPicked(Entity<ADTGunUpgradeTracerComponent> ent, ref ADTGunUpgradeTracerColorMessage args)
    {
        if (!ent.Comp.Adjustable)
            return;

        ent.Comp.BoltColor = args.Color;
        Dirty(ent);
    }

    private void RelayEvent<T>(Entity<ADTUpgradeableGunComponent> ent, ref T args) where T : notnull
    {
        foreach (var upgrade in GetUpgrades(ent))
        {
            RaiseLocalEvent(upgrade, ref args);
        }
    }

    private void OnAmmoShot(Entity<ADTUpgradeableGunComponent> ent, ref AmmoShotEvent args)
    {
        var ev = new ADTGunUpgradeShotEvent(ent.Owner, args.FiredProjectiles);
        foreach (var upgrade in GetUpgrades(ent))
        {
            RaiseLocalEvent(upgrade, ref ev);
        }
    }

    private void OnExamine(Entity<ADTUpgradeableGunComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(ADTUpgradeableGunComponent)))
        {
            args.PushMarkup(Loc.GetString("adt-gun-upgrade-examine-capacity", ("remaining", GetRemainingCapacity(ent))));

            foreach (var upgrade in GetUpgrades(ent))
            {
                if (upgrade.Comp.ExamineText != string.Empty)
                    args.PushMarkup(Loc.GetString(upgrade.Comp.ExamineText));
            }
        }
    }

    private void OnUpgradeExamine(Entity<ADTGunUpgradeComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("adt-gun-upgrade-examine-cost", ("cost", ent.Comp.Cost)));
    }

    public HashSet<Entity<ADTGunUpgradeComponent>> GetUpgrades(Entity<ADTUpgradeableGunComponent> ent)
    {
        var upgrades = new HashSet<Entity<ADTGunUpgradeComponent>>();
        if (!_container.TryGetContainer(ent, ent.Comp.UpgradesContainerId, out var container))
            return upgrades;

        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp<ADTGunUpgradeComponent>(contained, out var upgrade))
                upgrades.Add((contained, upgrade));
        }

        return upgrades;
    }

    public int GetRemainingCapacity(Entity<ADTUpgradeableGunComponent> ent)
    {
        var used = 0;
        foreach (var upgrade in GetUpgrades(ent))
        {
            used += upgrade.Comp.Cost;
        }

        return ent.Comp.MaxModCapacity - used;
    }
}

[Serializable, NetSerializable]
public sealed partial class ADTGunUpgradeRemoveDoAfterEvent : SimpleDoAfterEvent;

[ByRefEvent]
public record struct ADTGunUpgradeShotEvent(EntityUid Gun, List<EntityUid> Projectiles);
