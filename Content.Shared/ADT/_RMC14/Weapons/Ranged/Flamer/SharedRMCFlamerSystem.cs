// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.OnCollide;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

public abstract class SharedRMCFlamerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedRMCOnCollideSystem _onCollide = default!;
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SolutionTransferSystem _solutionTransfer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, MapInitEvent>(OnMapInit, after: new[] { typeof(SharedSolutionContainerSystem) });
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, GetAmmoCountEvent>(OnGetAmmoCount);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, EntInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, AttemptShootEvent>(OnAttemptShoot);

        SubscribeLocalEvent<RMCFlamerTankComponent, BeforeRangedInteractEvent>(OnFlamerTankBeforeRangedInteract);
        SubscribeLocalEvent<RMCFlamerTankComponent, ExaminedEvent>(OnFlamerTankExamined);

        SubscribeLocalEvent<RMCIgniterComponent, MapInitEvent>(OnIgniterMapInit, after: new[] { typeof(SharedSolutionContainerSystem) });
        SubscribeLocalEvent<RMCIgniterComponent, UniqueActionEvent>(OnIgniterUniqueAction);
        SubscribeLocalEvent<RMCIgniterComponent, IsHotEvent>(OnIgniterIsHot);
        SubscribeLocalEvent<RMCIgniterComponent, AttemptShootEvent>(OnIgniterAttemptShoot);
        SubscribeLocalEvent<RMCIgniterComponent, ExaminedEvent>(OnIgniterExamined);

        SubscribeLocalEvent<RMCFlamerChainComponent, ComponentShutdown>(OnFlamerChainShutdown);
    }

    private void OnMapInit(Entity<RMCFlamerAmmoProviderComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnTakeAmmo(Entity<RMCFlamerAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        args.Ammo.Add((ent, ent.Comp));
    }

    private void OnGetAmmoCount(Entity<RMCFlamerAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        if (!TryGetTankSolution(ent, out var solutionEnt, out _))
            return;

        var solution = solutionEnt.Value.Comp.Solution;
        args.Count = solution.Volume.Int();
        args.Capacity = solution.MaxVolume.Int();
    }

    private void OnInsertedIntoContainer(Entity<RMCFlamerAmmoProviderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        UpdateAppearance(ent);
    }

    private void OnRemovedFromContainer(Entity<RMCFlamerAmmoProviderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        UpdateAppearance(ent);
    }

    private void OnAttemptShoot(Entity<RMCFlamerAmmoProviderComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryGetTankSolution(ent, out var solution, out _) &&
            solution.Value.Comp.Solution.Volume >= ent.Comp.CostPer)
        {
            return;
        }

        args.Cancelled = true;

        var time = _timing.CurTime;
        if (time < ent.Comp.CantShootPopupLast + ent.Comp.CantShootPopupCooldown)
            return;

        ent.Comp.CantShootPopupLast = time;
        Dirty(ent);

        args.Message = Loc.GetString("rmc-flamer-empty");
    }

    private void OnFlamerTankBeforeRangedInteract(Entity<RMCFlamerTankComponent> tank, ref BeforeRangedInteractEvent args)
    {
        if (!args.CanReach || args.Handled)
            return;

        if (args.Target is not { } target)
            return;

        if (!_solution.TryGetSolution(tank.Owner, tank.Comp.SolutionId, out var tankSolutionEnt, out _))
            return;

        Entity<SolutionComponent> targetSolutionEnt;
        if (TryComp(target, out RMCFlamerBackpackComponent? backpack) &&
            _solution.TryGetSolution(target, backpack.SolutionId, out var backpackSolution))
        {
            targetSolutionEnt = backpackSolution.Value;
        }
        else if (TryComp(target, out RMCFlamerTankComponent? targetTank) &&
                 _solution.TryGetSolution(target, targetTank.SolutionId, out var targetTankSolution))
        {
            targetSolutionEnt = targetTankSolution.Value;
        }
        else if (HasComp<ReagentTankComponent>(target) &&
                 _solution.TryGetDrainableSolution(target, out var reagentTankSolutionEnt, out _))
        {
            targetSolutionEnt = reagentTankSolutionEnt.Value;
        }
        else
        {
            return;
        }

        args.Handled = true;
        Transfer(target, targetSolutionEnt, tank, tankSolutionEnt.Value, args.User);
    }

    private void OnFlamerTankExamined(Entity<RMCFlamerTankComponent> tank, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RMCFlamerTankComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-flamer-tank-examine-intensity", ("value", tank.Comp.MaxIntensity)));
            args.PushMarkup(Loc.GetString("rmc-flamer-tank-examine-duration", ("value", tank.Comp.MaxDuration)));
            args.PushMarkup(Loc.GetString("rmc-flamer-tank-examine-range", ("value", tank.Comp.MaxRange)));
        }
    }

    private void OnIgniterMapInit(Entity<RMCIgniterComponent> ent, ref MapInitEvent args)
    {
        _appearance.SetData(ent, RMCIgniterVisuals.Ignited, ent.Comp.Enabled);
    }

    private void OnIgniterUniqueAction(Entity<RMCIgniterComponent> ent, ref UniqueActionEvent args)
    {
        if (args.Handled || ent.Comp.Locked)
            return;

        args.Handled = true;
        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);

        _audio.PlayPredicted(ent.Comp.Sound, ent, args.UserUid);
        _appearance.SetData(ent, RMCIgniterVisuals.Ignited, ent.Comp.Enabled);
    }

    private void OnIgniterIsHot(Entity<RMCIgniterComponent> ent, ref IsHotEvent args)
    {
        args.IsHot = ent.Comp.Enabled;
    }

    protected virtual void OnIgniterAttemptShoot(Entity<RMCIgniterComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Enabled)
            args.Cancelled = true;
    }

    private void OnIgniterExamined(Entity<RMCIgniterComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Locked)
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.ExamineText), 1);
    }

    private void UpdateAppearance(Entity<RMCFlamerAmmoProviderComponent> ent)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        var volume = FixedPoint2.Zero;
        var maxVolume = FixedPoint2.Zero;
        var tank = false;
        if (TryGetTankSolution(ent, out var solutionEnt, out _))
        {
            var solution = solutionEnt.Value.Comp.Solution;
            volume = solution.Volume;
            maxVolume = solution.MaxVolume;
            tank = true;
        }

        _appearance.SetData(ent, AmmoVisuals.HasAmmo, volume > FixedPoint2.Zero, appearance);
        _appearance.SetData(ent, AmmoVisuals.AmmoCount, volume.Int(), appearance);
        _appearance.SetData(ent, AmmoVisuals.AmmoMax, maxVolume.Int(), appearance);
        _appearance.SetData(ent, AmmoVisuals.MagLoaded, tank, appearance);
        _appearance.SetData(ent, RMCFlamerVisualLayers.Strip, tank, appearance);
    }

    public void ShootFlamer(
        Entity<RMCFlamerAmmoProviderComponent> flamer,
        Entity<GunComponent> gun,
        EntityUid? user,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates)
    {
        if (!CanShootFlamer(flamer, fromCoordinates, toCoordinates, out var tiles, out var solution, out var reagent, out var tank))
            return;

        _audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);

        var cost = tiles.Count;
        if (reagent.FireSpread && cost > 2)
            cost = (int) Math.Ceiling(cost / 3.0f);

        solution.Value.Comp.Solution.RemoveSolution(flamer.Comp.CostPer * cost);
        _solution.UpdateChemicals(solution.Value);
        UpdateAppearance(flamer);

        if (_net.IsClient)
            return;

        var chain = Spawn();
        var chainComp = EnsureComp<RMCFlamerChainComponent>(chain);
        chainComp.Spawn = reagent.FireEntity;
        chainComp.Tiles = tiles;
        chainComp.Reagent = reagent.ID;
        chainComp.MaxIntensity = tank.Value.Comp.MaxIntensity;
        chainComp.MaxDuration = tank.Value.Comp.MaxDuration;
        chainComp.FuelPressure = flamer.Comp.CostPer.Int();
    }

    public bool TryGetPreviewTiles(
        Entity<RMCFlamerAmmoProviderComponent> flamer,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        [NotNullWhen(true)] out List<LineTile>? tiles)
    {
        return CanShootFlamer(flamer, fromCoordinates, toCoordinates, out tiles, out _, out _, out _);
    }

    public bool TryGetFuelColor(Entity<RMCFlamerAmmoProviderComponent> flamer, out Color color)
    {
        color = default;
        if (!TryGetTankSolution(flamer, out var solutionEnt, out _))
            return false;

        color = solutionEnt.Value.Comp.Solution.GetColor(_prototypes);
        return true;
    }

    private bool CanShootFlamer(
        Entity<RMCFlamerAmmoProviderComponent> flamer,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        [NotNullWhen(true)] out List<LineTile>? tiles,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solution,
        [NotNullWhen(true)] out ReagentPrototype? reagent,
        [NotNullWhen(true)] out Entity<RMCFlamerTankComponent>? tank)
    {
        tiles = null;
        reagent = null;
        if (!TryGetTankSolution(flamer, out solution, out tank))
            return false;

        var volume = solution.Value.Comp.Solution.Volume;
        if (volume < flamer.Comp.CostPer)
            return false;

        var fromMap = _transform.ToMapCoordinates(fromCoordinates);
        var toMap = _transform.ToMapCoordinates(toCoordinates);

        if (fromMap.MapId != toMap.MapId)
            return false;

        var delta = toMap.Position - fromMap.Position;
        if (delta.IsLengthZero())
            return false;

        var normalized = delta.Normalized();

        if (!solution.Value.Comp.Solution.Contents.TryFirstOrNull(out var firstReagent))
            return false;

        if (!_prototypes.TryIndex(firstReagent.Value.Reagent.Prototype, out reagent))
            return false;

        var maxRange = Math.Min(tank.Value.Comp.MaxRange, reagent.Radius);
        if (maxRange <= 0)
            return false;

        var range = Math.Min((volume / flamer.Comp.CostPer).Int(), maxRange);
        if (delta.Length() > maxRange)
            toMap = fromMap.Offset(normalized * range);

        fromCoordinates = _transform.ToCoordinates(fromCoordinates.EntityId, fromMap);
        toCoordinates = _transform.ToCoordinates(fromCoordinates.EntityId, toMap);

        fromCoordinates = _rmcMap.SnapToGrid(fromCoordinates);

        tiles = _line.DrawLine(fromCoordinates, toCoordinates, flamer.Comp.DelayPer, maxRange, out _, true, reagent.FireSpread);

        if (tiles.Count > 0)
            tiles.RemoveAt(0);

        var origin = _transform.ToMapCoordinates(fromCoordinates).Position;
        tiles.RemoveAll(tile => (_transform.ToMapCoordinates(tile.Coordinates).Position - origin).LengthSquared() < 0.25f);

        if (tiles.Count == 0)
        {
            tiles = null;
            return false;
        }

        return true;
    }

    private bool TryGetTankSolution(
        Entity<RMCFlamerAmmoProviderComponent> flamer,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEnt,
        [NotNullWhen(true)] out Entity<RMCFlamerTankComponent>? tankEnt)
    {
        solutionEnt = null;
        tankEnt = null;

        if (TryComp(flamer, out RMCFlamerTankComponent? tankComp))
        {
            tankEnt = (flamer, tankComp);
        }
        else if (_container.TryGetContainer(flamer, flamer.Comp.ContainerId, out var container) &&
                 container.ContainedEntities.TryFirstOrNull(out var tankId) &&
                 TryComp(tankId, out tankComp))
        {
            tankEnt = (tankId.Value, tankComp);
        }

        if (tankEnt is not { } tankValue)
            return false;

        return _solution.TryGetSolution(tankValue.Owner, tankValue.Comp.SolutionId, out solutionEnt, out _);
    }

    public void Transfer(
        EntityUid source,
        Entity<SolutionComponent> sourceSolutionEnt,
        Entity<RMCFlamerTankComponent> target,
        Entity<SolutionComponent> targetSolutionEnt,
        EntityUid user)
    {
        var tankSolution = targetSolutionEnt.Comp.Solution;
        var sourceSolution = sourceSolutionEnt.Comp.Solution;

        foreach (var content in sourceSolution.Contents)
        {
            if (target.Comp.ReagentWhitelist is { } whitelist && !whitelist.Contains(content.Reagent.Prototype))
            {
                _popup.PopupClient(Loc.GetString("rmc-flamer-tank-not-whitelisted", ("tank", target.Owner)), source, user);
                return;
            }

            if (_prototypes.TryIndex(content.Reagent.Prototype, out ReagentPrototype? reagent) &&
                (reagent.Intensity <= 0 || reagent.Duration <= 0 || reagent.Radius <= 0))
            {
                _popup.PopupClient(Loc.GetString("rmc-flamer-tank-not-potent-enough"), source, user);
                return;
            }
        }

        var data = new SolutionTransferData(
            user,
            source,
            sourceSolutionEnt,
            target,
            targetSolutionEnt,
            tankSolution.AvailableVolume);

        if (_solutionTransfer.Transfer(data) > FixedPoint2.Zero)
            _popup.PopupClient(Loc.GetString("rmc-flamer-refill", ("refilled", target.Owner)), source, user);
    }

    private void OnFlamerChainShutdown(Entity<RMCFlamerChainComponent> ent, ref ComponentShutdown args)
    {
        _onCollide.CleanupChain(ent.Comp.Chain);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var chains = EntityQueryEnumerator<RMCFlamerChainComponent>();
        while (chains.MoveNext(out var uid, out var comp))
        {
            if (comp.Tiles.Count == 0)
            {
                QueueDel(uid);
                continue;
            }

            comp.Chain ??= _onCollide.SpawnChain();

            foreach (var tile in comp.Tiles)
            {
                if (time < tile.At)
                    continue;

                comp.Tiles.Remove(tile);
                var fire = Spawn(comp.Spawn, tile.Coordinates);

                EnsureComp<RMCDamageOnCollideComponent>(fire, out var collide);
                _onCollide.SetChain((fire, collide), comp.Chain.Value);

                if (_rmcMap.HasAnchoredEntityEnumerator<TileFireComponent>(tile.Coordinates, out var oldTileFire) &&
                    oldTileFire.Owner != fire)
                {
                    QueueDel(oldTileFire.Owner);
                }

                if (_prototypes.TryIndex(comp.Reagent, out ReagentPrototype? reagent))
                {
                    var intensity = Math.Min(comp.MaxIntensity, reagent.Intensity);
                    var duration = Math.Min(comp.MaxDuration, reagent.Duration + (int) (comp.FuelPressure * reagent.DurationMod));
                    _rmcFlammable.SetIntensityDuration(fire, intensity, duration);

                    _rmcFlammable.SetVacuumBehaviour(fire, reagent.BurnsInVacuum, reagent.VacuumBurnout);
                }

                break;
            }
        }
    }
}
