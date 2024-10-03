using Content.Server.Explosion.EntitySystems;
using Content.Server.Stealth;
using Content.Shared.Actions;
using Content.Shared.ADT.Crawling;
using Content.Shared.ADT.Implants;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stealth.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.Interaction.Components;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;

namespace Content.Server.ADT.Implants;

public sealed class VisibleImplantSystem : SharedVisibleImplantSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly StealthSystem _stealth = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MantisDaggersComponent, MapInitEvent>(InitDaggers);
        SubscribeLocalEvent<MantisDaggersComponent, ComponentShutdown>(ShutdownDaggers);
        SubscribeLocalEvent<MantisDaggersComponent, ToggleMantisDaggersEvent>(OnToggleDaggers);

        SubscribeLocalEvent<MistralFistsComponent, MapInitEvent>(InitFists);
        SubscribeLocalEvent<MistralFistsComponent, ComponentShutdown>(ShutdownFists);

        SubscribeLocalEvent<SundownerShieldsComponent, MapInitEvent>(InitShields);
        SubscribeLocalEvent<SundownerShieldsComponent, ComponentShutdown>(ShutdownShields);
        SubscribeLocalEvent<SundownerShieldsComponent, ExplosionDownAttemptEvent>(OnSundownerExplosionDown);
        SubscribeLocalEvent<SundownerShieldsComponent, GetExplosionResistanceEvent>(OnGetExplosionResistance);
        SubscribeLocalEvent<SundownerShieldsComponent, DamageModifyEvent>(OnDamage);
        SubscribeLocalEvent<SundownerShieldsComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<SundownerShieldsComponent, ToggleSundownerShieldsEvent>(OnToggleShields);

        SubscribeLocalEvent<GigaMusclesComponent, MapInitEvent>(OnMusclesInit);
        SubscribeLocalEvent<GigaMusclesComponent, ComponentShutdown>(OnMusclesShutdown);
        SubscribeLocalEvent<GigaMusclesComponent, ToggleMusclesEvent>(OnMusclesToggle);

        SubscribeLocalEvent<ToggleableStealthComponent, MapInitEvent>(InitStealth);
        SubscribeLocalEvent<ToggleableStealthComponent, ComponentShutdown>(ShutdownStealth);
        SubscribeLocalEvent<ToggleableStealthComponent, ToggleCompStealthEvent>(OnToggleStealth);
    }

    #region Daggers
    private void InitDaggers(EntityUid uid, MantisDaggersComponent comp, MapInitEvent args)
    {
        _action.AddAction(uid, ref comp.ActionEntity, "ActionToggleMantisDaggers");
        comp.Container = _container.EnsureContainer<Container>(uid, "MantisDaggersContainer");
        comp.InnateWeapon = Spawn("ADTMantisDaggers", Transform(uid).Coordinates);
        _container.Insert(comp.InnateWeapon.Value, comp.Container, force: true);
        Dirty(uid, comp);
    }

    private void ShutdownDaggers(EntityUid uid, MantisDaggersComponent comp, ComponentShutdown args)
    {
        _action.RemoveAction(uid, comp.ActionEntity);
        QueueDel(comp.InnateWeapon);
        _container.ShutdownContainer(comp.Container);
    }

    private void OnToggleDaggers(EntityUid uid, MantisDaggersComponent comp, ToggleMantisDaggersEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MeleeWeaponComponent>(uid, out var melee))
            return;

        if (!comp.Active)
        {
            _appearance.SetData(uid, MantisDaggersVisuals.Active, true);
            _appearance.SetData(uid, MantisDaggersVisuals.Inactive, false);
        }
        else
        {
            _appearance.SetData(uid, MantisDaggersVisuals.Active, false);
            _appearance.SetData(uid, MantisDaggersVisuals.Inactive, true);
        }
        comp.Active = !comp.Active;
        _audio.PlayEntity(comp.Sound, Filter.Pvs(uid), uid, true);
        Dirty(uid, comp);
    }
    #endregion

    #region Fists
    private void InitFists(EntityUid uid, MistralFistsComponent comp, MapInitEvent args)
    {
        comp.Container = _container.EnsureContainer<Container>(uid, "MantisDaggersContainer");
        comp.InnateWeapon = Spawn("ADTMistralFists", Transform(uid).Coordinates);
        _container.Insert(comp.InnateWeapon.Value, comp.Container, force: true);
        Dirty(uid, comp);
        _appearance.SetData(uid, MistralFistsVisuals.Active, true);
        _appearance.SetData(uid, MistralFistsVisuals.Inactive, false);
    }

    private void ShutdownFists(EntityUid uid, MistralFistsComponent comp, ComponentShutdown args)
    {
        QueueDel(comp.InnateWeapon);
        _container.ShutdownContainer(comp.Container);
        _appearance.SetData(uid, MistralFistsVisuals.Active, false);
        _appearance.SetData(uid, MistralFistsVisuals.Inactive, true);
    }
    #endregion

    #region Sundowner
    private void InitShields(EntityUid uid, SundownerShieldsComponent comp, MapInitEvent args)
    {
        _action.AddAction(uid, ref comp.ActionEntity, "ActionToggleSundownerShields");
        comp.Container = _container.EnsureContainer<Container>(uid, "MantisDaggersContainer");
        comp.InnateWeapon = Spawn("ADTMistralFists", Transform(uid).Coordinates);
        _container.Insert(comp.InnateWeapon.Value, comp.Container, force: true);
        Dirty(uid, comp);
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
        _appearance.SetData(uid, SundownerShieldsVisuals.Open, true);
        _appearance.SetData(uid, SundownerShieldsVisuals.Closed, false);
    }

    private void ShutdownShields(EntityUid uid, SundownerShieldsComponent comp, ComponentShutdown args)
    {
        _action.RemoveAction(uid, comp.ActionEntity);
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
        QueueDel(comp.InnateWeapon);
        _container.ShutdownContainer(comp.Container);
        _appearance.SetData(uid, SundownerShieldsVisuals.Open, false);
        _appearance.SetData(uid, SundownerShieldsVisuals.Closed, false);
    }

    private void OnSundownerExplosionDown(EntityUid uid, SundownerShieldsComponent comp, ref ExplosionDownAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnGetExplosionResistance(EntityUid uid, SundownerShieldsComponent comp, ref GetExplosionResistanceEvent args)
    {
        if (comp.Active)
        {
            args.DamageCoefficient = 0f;
        }
        else
            args.DamageCoefficient = 0.5f;
    }

    private void OnDamage(EntityUid uid, SundownerShieldsComponent comp, ref DamageModifyEvent args)
    {
        if (TryComp<ArmorComponent>(comp.InnateWeapon, out var armor))
        {
            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, armor.Modifiers);
        }

        if (comp.Active)
        {
            if (!args.OriginalDamage.AnyPositive() || args.OriginalDamage.GetTotal() <= 5)
                return;
            _explosion.QueueExplosion(_transform.GetMapCoordinates(uid), "Default", 2f, 1f, 0f, null, canCreateVacuum: false);
            comp.DisableTime = _timing.CurTime + TimeSpan.FromSeconds(2);
        }
    }

    private void OnRefreshMovespeed(EntityUid uid, SundownerShieldsComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (!comp.Active)
            args.ModifySpeed(0.8f, 0.8f);
        else
            args.ModifySpeed(0.4f, 0.4f);
    }

    private void OnToggleShields(EntityUid uid, SundownerShieldsComponent comp, ToggleSundownerShieldsEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        ToggleShields(uid, comp);
    }

    private void ToggleShields(EntityUid uid, SundownerShieldsComponent comp)
    {
        comp.Active = !comp.Active;
        _appearance.SetData(uid, SundownerShieldsVisuals.Closed, comp.Active);
        _appearance.SetData(uid, SundownerShieldsVisuals.Open, !comp.Active);
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
        _action.SetToggled(comp.ActionEntity, comp.Active);
        _audio.PlayEntity(comp.Sound, Filter.Pvs(uid), uid, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SundownerShieldsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.DisableTime == TimeSpan.Zero)
                continue;
            if (comp.DisableTime <= _timing.CurTime)
            {
                comp.DisableTime = TimeSpan.Zero;
                ToggleShields(uid, comp);
                _action.SetCooldown(comp.ActionEntity, TimeSpan.FromSeconds(30));
            }
        }
    }
    #endregion

    #region Muscles
    private void OnMusclesInit(EntityUid uid, GigaMusclesComponent comp, MapInitEvent args)
    {
        _action.AddAction(uid, ref comp.ActionEntity, "ActionToggleMuscles");
    }

    private void OnMusclesShutdown(EntityUid uid, GigaMusclesComponent comp, ComponentShutdown args)
    {
        _action.RemoveAction(comp.ActionEntity);
    }

    private void OnMusclesToggle(EntityUid uid, GigaMusclesComponent comp, ToggleMusclesEvent args)
    {
        if (comp.SpawnedEntity == null)
        {
            if (_inventory.TryGetSlotEntity(uid, "outerClothing", out var equipped))
                _inventory.TryUnequip(uid, "outerClothing", true);
            var muscles = Spawn("ADTClothingGigaMuscles");
            if (_inventory.TryEquip(uid, muscles, "outerClothing", force: true))
            {
                comp.SpawnedEntity = muscles;
                EnsureComp<UnremoveableComponent>(muscles);
            }

            else
                QueueDel(muscles);
        }
        else
        {
            QueueDel(comp.SpawnedEntity);
            comp.SpawnedEntity = null;
        }
        _audio.PlayEntity(comp.Sound, Filter.Pvs(uid), uid, true);
    }

    #endregion

    #region Stealth
    private void InitStealth(EntityUid uid, ToggleableStealthComponent comp, MapInitEvent args)
    {
        _action.AddAction(uid, ref comp.ActionEntity, "ActionToggleCompStealth");
    }

    private void ShutdownStealth(EntityUid uid, ToggleableStealthComponent comp, ComponentShutdown args)
    {
        _action.RemoveAction(comp.ActionEntity);
    }

    private void OnToggleStealth(EntityUid uid, ToggleableStealthComponent comp, ToggleCompStealthEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        if (!comp.Toggled)
        {
            if (HasComp<StealthComponent>(uid))
                return;
            var stealth = EnsureComp<StealthComponent>(uid);
            stealth.MinVisibility = -1f;
            _stealth.SetVisibility(uid, -1f);
            Dirty(uid, stealth);
        }
        else
        {
            RemCompDeferred<StealthComponent>(uid);
        }
        comp.Toggled = !comp.Toggled;
    }
    #endregion

}

