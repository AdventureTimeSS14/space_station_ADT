using Content.Shared.Timing;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Upgrades.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Damage.Components;
using Robust.Shared.GameObjects;
using Content.Shared.HunterEye;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.ADT.Fauna;

namespace Content.Shared.Weapons.Ranged.Upgrades;

public sealed class GunUpgradeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainers = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly DamageableSystem? _damageable;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<UpgradeableGunComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<UpgradeableGunComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<UpgradeableGunComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<UpgradeableGunComponent, GunRefreshModifiersEvent>(RelayEvent);
        SubscribeLocalEvent<UpgradeableGunComponent, GunShotEvent>(RelayEvent);

        SubscribeLocalEvent<GunUpgradeFireRateComponent, GunRefreshModifiersEvent>(OnFireRateRefresh);
        SubscribeLocalEvent<GunComponentUpgrateComponent, GunRefreshModifiersEvent>(OnCompsRefresh);
        SubscribeLocalEvent<GunUpgradeSpeedComponent, GunRefreshModifiersEvent>(OnSpeedRefresh);
        SubscribeLocalEvent<GunUpgradeDamageComponent, GunShotEvent>(OnDamageGunShot);
        SubscribeLocalEvent<GunUpgradeComponentsComponent, GunShotEvent>(OnDamageGunShotComps);
        SubscribeLocalEvent<GunUpgradeVampirismComponent, GunShotEvent>(OnVampirismGunShot);
        SubscribeLocalEvent<ProjectileVampirismComponent, ProjectileHitEvent>(OnVampirismProjectileHit);
        SubscribeLocalEvent<GunUpgradeReagentAddComponent, GunShotEvent>(OnReagentAddGunShot);
        SubscribeLocalEvent<ProjectileReagentAddComponent, ProjectileHitEvent>(OnReagentAddProjectileHit);
        SubscribeLocalEvent<GunUpgradeBloodDrunkerComponent, GunShotEvent>(OnBloodDrunkerGunShot);
        SubscribeLocalEvent<ProjectileBloodDrunkerComponent, ProjectileHitEvent>(OnBloodDrunkerProjectileHit);
    }

    private void RelayEvent<T>(Entity<UpgradeableGunComponent> ent, ref T args) where T : notnull
    {
        foreach (var upgrade in GetCurrentUpgrades(ent))
        {
            RaiseLocalEvent(upgrade, ref args);
        }
    }

    private void OnExamine(Entity<UpgradeableGunComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(UpgradeableGunComponent)))
        {
            foreach (var upgrade in GetCurrentUpgrades(ent))
            {
                args.PushMarkup(Loc.GetString(upgrade.Comp.ExamineText));
            }
        }
    }

    private void OnStartup(Entity<UpgradeableGunComponent> ent, ref ComponentStartup args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.UpgradesContainerId);
    }

    private void OnAfterInteractUsing(Entity<UpgradeableGunComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || !TryComp<GunUpgradeComponent>(args.Used, out var upgradeComponent))
            return;

        if (GetCurrentUpgrades(ent).Count >= ent.Comp.MaxUpgradeCount)
        {
            _popup.PopupPredicted(Loc.GetString("upgradeable-gun-popup-upgrade-limit"), ent, args.User);
            return;
        }

        if (_entityWhitelist.IsWhitelistFail(ent.Comp.Whitelist, args.Used))
            return;



        _audio.PlayPredicted(ent.Comp.InsertSound, ent, args.User);
        _gun.RefreshModifiers(ent.Owner);
        args.Handled = _container.Insert(args.Used, _container.GetContainer(ent, ent.Comp.UpgradesContainerId));
    }

    private void OnFireRateRefresh(Entity<GunUpgradeFireRateComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.FireRate *= ent.Comp.Coefficient;
    }

    private void OnCompsRefresh(Entity<GunComponentUpgrateComponent> ent, ref GunRefreshModifiersEvent args)
    {
        EntityManager.AddComponents(args.Gun, ent.Comp.Components);
    }

    private void OnSpeedRefresh(Entity<GunUpgradeSpeedComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.ProjectileSpeed *= ent.Comp.Coefficient;
    }

    private void OnDamageGunShot(Entity<GunUpgradeDamageComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (TryComp<ProjectileComponent>(ammo, out var proj))
                proj.Damage += ent.Comp.Damage;
        }
    }
    private void OnDamageGunShotComps(Entity<GunUpgradeComponentsComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (TryComp<ProjectileComponent>(ammo, out var proj))
                EntityManager.AddComponents(ammo.Value, ent.Comp.Components);
        }
    }

    private void OnVampirismGunShot(Entity<GunUpgradeVampirismComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (TryComp<ProjectileComponent>(ammo, out var proj))
            {
                var comp = EnsureComp<ProjectileVampirismComponent>(ammo.Value);
                comp.DamageOnHit = ent.Comp.DamageOnHit;
            }
        }
    }

    private void OnVampirismProjectileHit(Entity<ProjectileVampirismComponent> ent, ref ProjectileHitEvent args)
    {
        if (!HasComp<MobStateComponent>(args.Target))
            return;
        _damage.TryChangeDamage(args.Shooter, ent.Comp.DamageOnHit);
    }

    private void OnReagentAddGunShot(Entity<GunUpgradeReagentAddComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (TryComp<ProjectileComponent>(ammo, out var proj))
            {
                var comp = EnsureComp<ProjectileReagentAddComponent>(ammo.Value);
                comp.ReagentOnHit = ent.Comp.ReagentOnHit;
                comp.ReagentCount = ent.Comp.ReagentCount;
            }
        }
    }

    private void OnReagentAddProjectileHit(Entity<ProjectileReagentAddComponent> ent, ref ProjectileHitEvent args)
    {
        if (!HasComp<MobStateComponent>(args.Target))
            return;

        if (args.Shooter.HasValue && TryComp<SolutionContainerManagerComponent>(args.Shooter.Value, out var container))
        {
            if (_solutionContainers.TryGetSolution(args.Shooter.Value, "chemicals", out var solution))
            {
                var reagent = new ReagentQuantity(ent.Comp.ReagentOnHit, ent.Comp.ReagentCount);
                _solutionContainers.TryAddReagent(solution.Value, reagent, out var acceptedAmount);
            }
        }
    }

    private void OnBloodDrunkerGunShot(Entity<GunUpgradeBloodDrunkerComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (TryComp<ProjectileComponent>(ammo, out var proj))
            {
                var comp = EnsureComp<ProjectileBloodDrunkerComponent>(ammo.Value);
            }
        }
    }

    private void OnBloodDrunkerProjectileHit(Entity<ProjectileBloodDrunkerComponent> ent , ref ProjectileHitEvent args)
    {

        if (!HasComp<FaunaComponent>(args.Target))
            return;

        if (args.Shooter.HasValue && TryComp<MobStateComponent>(args.Shooter.Value, out var comp))
        {
            _statusEffect.TryAddStatusEffect<IgnoreSlowOnDamageComponent>(args.Shooter.Value, "Adrenaline", TimeSpan.FromSeconds(10), true);
            _statusEffect.TryAddStatusEffect<HunterEyeDamageReductionComponent>(args.Shooter.Value, "SDHunterEye", TimeSpan.FromSeconds(1), true);
            _statusEffect.TryRemoveStatusEffect(args.Shooter.Value, "Stun");
        }
    }

    public HashSet<Entity<GunUpgradeComponent>> GetCurrentUpgrades(Entity<UpgradeableGunComponent> ent)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.UpgradesContainerId, out var container))
            return new HashSet<Entity<GunUpgradeComponent>>();

        var upgrades = new HashSet<Entity<GunUpgradeComponent>>();
        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp<GunUpgradeComponent>(contained, out var upgradeComp))
                upgrades.Add((contained, upgradeComp));
        }

        return upgrades;
    }

    public IEnumerable<ProtoId<TagPrototype>> GetCurrentUpgradeTags(Entity<UpgradeableGunComponent> ent)
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
