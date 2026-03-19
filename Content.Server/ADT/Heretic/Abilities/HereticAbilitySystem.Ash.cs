using System.Linq;
using System.Threading.Tasks;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Heretic;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Temperature.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    private void SubscribeAsh()
    {
        SubscribeLocalEvent<HereticComponent, EventHereticAshenShift>(OnJaunt);
        SubscribeLocalEvent<GhoulComponent, EventHereticAshenShift>(OnJauntGhoul);

        SubscribeLocalEvent<HereticComponent, EventHereticVolcanoBlast>(OnVolcano);
        SubscribeLocalEvent<HereticComponent, EventHereticNightwatcherRebirth>(OnNWRebirth);
        SubscribeLocalEvent<HereticComponent, EventHereticFlames>(OnFlames);
        SubscribeLocalEvent<HereticComponent, EventHereticCascade>(OnCascade);

        SubscribeLocalEvent<HereticComponent, HereticAscensionAshEvent>(OnAscensionAsh);
    }

    private void OnJaunt(Entity<HereticComponent> ent, ref EventHereticAshenShift args)
    {
        if (TryUseAbility(ent, args) && TryDoJaunt(ent))
            args.Handled = true;
    }
    private void OnJauntGhoul(Entity<GhoulComponent> ent, ref EventHereticAshenShift args)
    {
        if (TryUseAbility(ent, args) && TryDoJaunt(ent))
            args.Handled = true;
    }
    private bool TryDoJaunt(EntityUid ent)
    {
        Spawn("PolymorphAshJauntAnimation", Transform(ent).Coordinates);
        var urist = _poly.PolymorphEntity(ent, "AshJaunt");
        if (urist == null)
            return false;

        return true;
    }

    private void OnVolcano(Entity<HereticComponent> ent, ref EventHereticVolcanoBlast args)
    {
        if (!TryUseAbility(ent, args))
            return;

        var ignoredTargets = new List<EntityUid>();

        // all ghouls are immune to heretic shittery
        foreach (var e in EntityQuery<GhoulComponent>())
            ignoredTargets.Add(e.Owner);

        // all heretics with the same path are also immune
        foreach (var e in EntityQuery<HereticComponent>())
            if (e.CurrentPath == ent.Comp.CurrentPath)
                ignoredTargets.Add(e.Owner);

        if (!_splitball.Spawn(ent, ignoredTargets))
            return;

        if (ent.Comp is { Ascended: true, CurrentPath: "Ash" }) // will only work on ash path
            _flammable.AdjustFireStacks(ent, 20f, ignite: true);

        args.Handled = true;
    }
    private void OnNWRebirth(Entity<HereticComponent> ent, ref EventHereticNightwatcherRebirth args)
    {
        if (!TryUseAbility(ent, args))
            return;

        if (TryComp<FlammableComponent>(ent, out var flamComp) && flamComp.OnFire)
        {
            _flammable.Extinguish(ent.Owner, flamComp);

            if (TryComp<TemperatureComponent>(ent, out var tempComp))
                _temperature.ForceChangeTemperature(ent.Owner, 293.15f, tempComp); // 20°C
        }

        var power = ent.Comp.CurrentPath == "Ash" ? ent.Comp.PathStage : 4f;
        var lookup = _lookup.GetEntitiesInRange(ent, power);

        foreach (var look in lookup)
        {
            if (look == ent.Owner)
                continue;

            if ((TryComp<HereticComponent>(look, out var th) && th.CurrentPath == ent.Comp.CurrentPath)
            || HasComp<GhoulComponent>(look))
                continue;

            if (TryComp<FlammableComponent>(look, out var flam))
            {
                bool targetDamageable = TryComp<DamageableComponent>(look, out var targetDmgc);

                if (flam.OnFire && targetDamageable)
                {
                    _flammable.AdjustFireStacks(look, power, flam, ignite: true);

                    var fireDamage = new DamageSpecifier();
                    fireDamage.DamageDict["Heat"] = power * 2;
                    _damageable.TryChangeDamage((look, targetDmgc), fireDamage, true, false, origin: ent.Owner);

                    // Лечим еретика за каждую подожжённую цель
                    bool hereticDamageable = TryComp<DamageableComponent>(ent.Owner, out var hereticDmgc);
                    if (hereticDamageable)
                    {
                        _stam.TryTakeStamina(ent.Owner, -(10 + power));

                        var totalHeal = 10f + power;
                        var healSpec = new DamageSpecifier();
                        var damageTypes = hereticDmgc!.Damage.DamageDict.Keys.ToList();
                        if (damageTypes.Count > 0)
                        {
                            var healPerType = totalHeal / damageTypes.Count;
                            foreach (var key in damageTypes)
                            {
                                healSpec.DamageDict[key] = -healPerType;
                            }

                            _damageable.TryChangeDamage((ent.Owner, hereticDmgc), healSpec, true, false, origin: ent.Owner);
                        }
                    }

                    // Проверяем, не перешла ли цель в крит после получения урона, и добиваем её
                    if (TryComp<MobStateComponent>(look, out var mobstat))
                    {
                        if (mobstat.CurrentState == MobState.Critical)
                        {
                            if (_mobThresholdSystem.TryGetThresholdForState(look, MobState.Dead, out var damage))
                            {
                                var damageNeeded = damage.Value - targetDmgc!.TotalDamage;
                                if (damageNeeded > 0)
                                {
                                    DamageSpecifier dspec = new();
                                    dspec.DamageDict["Heat"] = damageNeeded;
                                    _damageable.ChangeDamage(look, dspec, true, origin: ent.Owner);
                                }
                            }
                        }
                    }
                }
            }
        }

        args.Handled = true;
    }
    private void OnFlames(Entity<HereticComponent> ent, ref EventHereticFlames args)
    {
        if (!TryUseAbility(ent, args))
            return;

        EnsureComp<HereticFlamesComponent>(ent);

        if (ent.Comp.Ascended)
            _flammable.AdjustFireStacks(ent, 20f, ignite: true);

        args.Handled = true;
    }
    private void OnCascade(Entity<HereticComponent> ent, ref EventHereticCascade args)
    {
        if (!TryUseAbility(ent, args) || !Transform(ent).GridUid.HasValue)
            return;

        CombustArea(ent, 9, false);

        if (ent.Comp.Ascended)
            _flammable.AdjustFireStacks(ent, 20f, ignite: true);

        args.Handled = true;
    }


    private void OnAscensionAsh(Entity<HereticComponent> ent, ref HereticAscensionAshEvent args)
    {
        RemComp<TemperatureComponent>(ent);
        RemComp<TemperatureSpeedComponent>(ent);
        RemComp<RespiratorComponent>(ent);
        RemComp<BarotraumaComponent>(ent);

        // fire immunity
        var flam = EnsureComp<FlammableComponent>(ent);
        flam.Damage = new(); // reset damage dict
        // this does NOT protect you against lasers and whatnot. for now. when i figure out THIS STUPID FUCKING LIMB SYSTEM!!!
        // regards.
    }

    #region Helper methods

    [ValidatePrototypeId<EntityPrototype>] private static readonly EntProtoId FirePrototype = "HereticFireAA";

    public async Task CombustArea(EntityUid ent, int range = 1, bool hollow = true)
    {
        // we need this beacon in order for damage box to not break apart
        var beacon = Spawn(null, _xform.GetMapCoordinates((EntityUid) ent));

        for (int i = 0; i <= range; i++)
        {
            SpawnFireBox(beacon, range: i, hollow);
            await Task.Delay((int) 500f);
        }

        EntityManager.DeleteEntity(beacon); // cleanup
    }

    public void SpawnFireBox(EntityUid relative, int range = 0, bool hollow = true)
    {
        if (range == 0)
        {
            Spawn(FirePrototype, Transform(relative).Coordinates);
            return;
        }

        var xform = Transform(relative);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var gridEnt = ((EntityUid) xform.GridUid, grid);

        // get tile position of our entity
        if (!_xform.TryGetGridTilePosition(relative, out var tilePos))
            return;

        // make a box
        var pos = _map.TileCenterToVector(gridEnt, tilePos);
        var confines = new Box2(pos, pos).Enlarged(range);
        var box = _map.GetLocalTilesIntersecting(relative, grid, confines).ToList();

        // hollow it out if necessary
        if (hollow)
        {
            var confinesS = new Box2(pos, pos).Enlarged(Math.Max(range - 1, 0));
            var boxS = _map.GetLocalTilesIntersecting(relative, grid, confinesS).ToList();
            box = box.Where(b => !boxS.Contains(b)).ToList();
        }

        // fill the box
        foreach (var tile in box)
        {
            Spawn(FirePrototype, _map.GridTileToWorld((EntityUid) xform.GridUid, grid, tile.GridIndices));
        }
    }

    #endregion
}
