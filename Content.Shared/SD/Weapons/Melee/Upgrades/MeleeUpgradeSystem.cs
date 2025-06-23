using Content.Shared.Interaction;
using Content.Shared.Examine;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Content.Shared.StatusEffect;
using Content.Shared.ADT.Fauna;
using Content.Shared.HunterEye;
using Content.Shared.Mobs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Melee.Upgrades.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Effects;
using Content.Shared.Audio;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Timing;
using Robust.Shared.Log;
using System.Linq;

namespace Content.Shared.Weapons.Melee.Upgrades;

public sealed class MeleeUpgradeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UpgradeableMeleeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<UpgradeableMeleeComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<UpgradeableMeleeComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<MeleeComponentUpgradeComponent, MeleeRefreshModifiersEvent>(OnCompsRefresh);

        SubscribeLocalEvent<UpgradeableMeleeComponent, MeleeHitEvent>(RelayEvent);
        SubscribeLocalEvent<UpgradeableMeleeComponent, GetMeleeDamageEvent>(RelayEvent);
        SubscribeLocalEvent<UpgradeableMeleeComponent, GetMeleeSpeedEvent>(RelayEvent);

        SubscribeLocalEvent<MeleeDamageUpgradeComponent, GetMeleeDamageEvent>(OnDamageUpgrade);
        SubscribeLocalEvent<MeleeSpeedUpgradeComponent, GunRefreshModifiersEvent>(OnSpeedUpgrade);
        SubscribeLocalEvent<MeleeBloodDrunkerUpgradeComponent, MeleeHitEvent>(OnBloodDrunkerUpgrade);
    }

    private void OnCompsRefresh(Entity<MeleeComponentUpgradeComponent> ent, ref MeleeRefreshModifiersEvent args)
    {
        EntityManager.AddComponents(args.Melee, ent.Comp.Components);
    }

    private void OnStartup(Entity<UpgradeableMeleeComponent> ent, ref ComponentStartup args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.UpgradesContainerId);
    }

    private void OnExamine(Entity<UpgradeableMeleeComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(UpgradeableMeleeComponent)))
        {
            foreach (var upgrade in GetCurrentUpgrades(ent))
            {
                args.PushMarkup(Loc.GetString(upgrade.Comp.ExamineText));
            }
        }
    }

    private void OnAfterInteractUsing(Entity<UpgradeableMeleeComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || !TryComp<MeleeUpgradeComponent>(args.Used, out var upgradeComp))
            return;

        if (upgradeComp.Tags.Count > 0 && HasUpgradeWithTag(ent, upgradeComp.Tags.FirstOrDefault()))
        {
            _popup.PopupClient(Loc.GetString("upgradeable-melee-popup-duplicate-upgrade"), args.User, args.User);
            return;
        }

        if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, args.Used))
            return;

        var container = _container.GetContainer(ent, ent.Comp.UpgradesContainerId);
        if (!_container.Insert(args.Used, container))
            return;

        _audio.PlayPredicted(ent.Comp.InsertSound, ent, args.User);

        var meleeEv = new MeleeRefreshModifiersEvent(ent);
        RaiseLocalEvent(ent, meleeEv);

        _gun.RefreshModifiers(ent.Owner);

        args.Handled = _container.Insert(args.Used, _container.GetContainer(ent, ent.Comp.UpgradesContainerId));
    }

    private bool HasUpgradeWithTag(Entity<UpgradeableMeleeComponent> ent, ProtoId<TagPrototype>? upgradeTag)
    {
        if (upgradeTag == null)
            return false;

        var container = _container.GetContainer(ent, ent.Comp.UpgradesContainerId);
        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp<MeleeUpgradeComponent>(contained, out var upgradeComp) &&
                upgradeComp.Tags.Contains(upgradeTag.Value))
                return true;
        }
        return false;
    }

    private void RelayEvent<T>(Entity<UpgradeableMeleeComponent> ent, ref T args) where T : notnull
    {
        foreach (var upgrade in GetCurrentUpgrades(ent))
        {
            RaiseLocalEvent(upgrade, ref args);
        }
    }

    private void OnDamageUpgrade(Entity<MeleeDamageUpgradeComponent> ent, ref GetMeleeDamageEvent args)
    {
        args.Damage += ent.Comp.DamageBonus;
    }

    private void OnSpeedUpgrade(Entity<MeleeSpeedUpgradeComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var oldFireRate = args.FireRate;
        args.FireRate *= ent.Comp.SpeedMultiplier;
        Logger.Debug($"Speed upgrade applied. Old fire rate: {oldFireRate}, New fire rate: {args.FireRate}");
    }

    private void OnBloodDrunkerUpgrade(Entity<MeleeBloodDrunkerUpgradeComponent> ent, ref MeleeHitEvent args)
    {
        // В SharedDamageMarkerSystem прописано как это улучшение работает.
    }

    /// <summary>
    /// Получает все текущие улучшения для оружия
    /// </summary>
    public HashSet<Entity<MeleeUpgradeComponent>> GetCurrentUpgrades(Entity<UpgradeableMeleeComponent> ent)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.UpgradesContainerId, out var container))
            return new HashSet<Entity<MeleeUpgradeComponent>>();

        var upgrades = new HashSet<Entity<MeleeUpgradeComponent>>();
        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp<MeleeUpgradeComponent>(contained, out var upgradeComp))
                upgrades.Add((contained, upgradeComp));
        }

        return upgrades;
    }
    public IEnumerable<ProtoId<TagPrototype>> GetCurrentUpgradeTags(Entity<UpgradeableMeleeComponent> ent)
    {
        foreach (var upgrade in GetCurrentUpgrades(ent))
        {
            foreach (var tag in upgrade.Comp.Tags)
            {
                yield return tag;
            }
        }
    }
}
/// <summary>
/// Событие для получения модифицированной скорости атаки
/// </summary>
public sealed class GetMeleeSpeedEvent : EntityEventArgs
{
    public float Speed;

    public GetMeleeSpeedEvent(float baseSpeed)
    {
        Speed = baseSpeed;
    }
}

public sealed class MeleeRefreshModifiersEvent : EntityEventArgs
{
    public EntityUid Melee;

    public MeleeRefreshModifiersEvent(EntityUid melee)
    {
        Melee = melee;
    }
}
