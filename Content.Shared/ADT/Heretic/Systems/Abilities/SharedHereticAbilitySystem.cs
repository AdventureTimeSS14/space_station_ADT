//

using Content.Shared.Body;
using Content.Shared.Damage.Components;
using System.Linq;
using Content.Shared.ADT.Heretic.Common;
using Content.Shared.FixedPoint;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Magic.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Roles;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem : EntitySystem
{
    [Dependency] private SharedMapSystem _mapMan = default!;
    [Dependency] private ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private INetManager _net = default!;

    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected EntityLookupSystem Lookup = default!;
    [Dependency] protected StatusEffectsSystem Status = default!;
    [Dependency] protected SharedVoidCurseSystem Voidcurse = default!;
    [Dependency] protected SharedHereticSystem Heretic = default!;

    [Dependency] private StatusEffectNew.StatusEffectsSystem _statusNew = default!;
    [Dependency] private SharedProjectileSystem _projectile = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throw = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedStarMarkSystem _starMark = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedBloodstreamSystem _blood = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    [Dependency] protected SharedPopupSystem Popup = default!;

    public static readonly DamageSpecifier AllDamage = new()
    {
        DamageDict =
        {
            {"Blunt", 1},
            {"Slash", 1},
            {"Piercing", 1},
            {"Heat", 1},
            {"Cold", 1},
            {"Shock", 1},
            {"Asphyxiation", 1},
            {"Bloodloss", 1},
            {"Caustic", 1},
            {"Poison", 1},
            {"Radiation", 1},
            {"Cellular", 1},
            {"Holy", 1},
        },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAsh();
        SubscribeBlade();
        SubscribeRust();
        SubscribeCosmos();
        SubscribeVoid();
        SubscribeFlesh();
        SubscribeSide();

        SubscribeLocalEvent<HereticActionComponent, BeforeCastSpellEvent>(OnBeforeCast);
    }

    protected List<Entity<MobStateComponent>> GetNearbyPeople(EntityUid ent,
        float range,
        string? path,
        EntityCoordinates? coords = null,
        bool checkNullRod = true)
    {
        var list = new List<Entity<MobStateComponent>>();
        var lookup = Lookup.GetEntitiesInRange<MobStateComponent>(coords ?? Transform(ent).Coordinates, range);

        foreach (var look in lookup)
        {
            // ignore heretics with the same path*, affect everyone else
            if (Heretic.TryGetHereticComponent(look, out var th, out _) && th.CurrentPath == path ||
                HasComp<GhoulComponent>(look))
                continue;

            if (!HasComp<StatusEffectsComponent>(look))
                continue;

            if (checkNullRod)
            {
                var ev = new BeforeCastTouchSpellEvent(look, false);
                RaiseLocalEvent(look, ev, true);
                if (ev.Cancelled)
                    continue;
            }

            list.Add(look);
        }

        return list;
    }

    public bool TryUseAbility(BaseActionEvent args, bool handle = true)
    {
        if (args.Handled)
            return false;
        var ev = new BeforeCastSpellEvent(args.Performer);
        RaiseLocalEvent(args.Action, ref ev);
        var result = !ev.Cancelled;
        if (result && handle)
            args.Handled = true;
        return result;
    }

    private void OnBeforeCast(Entity<HereticActionComponent> ent, ref BeforeCastSpellEvent args)
    {
        if (HasComp<RustChargeComponent>(args.Performer))
        {
            args.Cancelled = true;
            return;
        }

        if (HasComp<GhoulComponent>(args.Performer) || HasComp<StarGazerComponent>(args.Performer))
            return;

        if (!Heretic.TryGetHereticComponent(args.Performer, out var heretic, out _))
        {
            args.Cancelled = true;
            return;
        }

        if (!ent.Comp.RequireMagicItem || heretic.Ascended)
            return;

        var ev = new CheckMagicItemEvent();
        RaiseLocalEvent(args.Performer, ev);

        if (ev.Handled)
            return;

        // Almost all of the abilites are serverside anyway
        if (_net.IsServer)
            Popup.PopupEntity(Loc.GetString("heretic-ability-fail-magicitem"), args.Performer, args.Performer);

        args.Cancelled = true;
    }

    private EntityUid? GetTouchSpell<TEvent, TComp>(EntityUid ent, ref TEvent args)
        where TEvent : InstantActionEvent, ITouchSpellEvent
        where TComp : Component
    {
        if (!TryUseAbility(args, false))
            return null;

        if (!TryComp(ent, out HandsComponent? hands) || hands.Hands.Count < 1)
            return null;

        args.Handled = true;

        var hasComp = false;

        foreach (var held in _hands.EnumerateHeld((ent, hands)))
        {
            if (!HasComp<TComp>(held))
                continue;

            hasComp = true;
            PredictedQueueDel(held);
        }

        if (hasComp || !_hands.TryGetEmptyHand((ent, hands), out var emptyHand))
            return null;

        var touch = PredictedSpawnAtPosition(args.TouchSpell, Transform(ent).Coordinates);

        if (_hands.TryPickup(ent, touch, emptyHand, animate: false, handsComp: hands))
            return touch;

        PredictedQueueDel(touch);
        return null;
    }

    protected EntityUid ShootProjectileSpell(EntityUid performer,
        EntityCoordinates coords,
        EntProtoId toSpawn,
        float speed,
        EntityUid? target)
    {
        var xform = Transform(performer);
        var fromCoords = xform.Coordinates;
        var toCoords = coords;

        var fromMap = _transform.ToMapCoordinates(fromCoords);
        var spawnCoords = _mapMan.TryFindGridAt(fromMap, out var gridUid, out _)
            ? _transform.WithEntityId(fromCoords, gridUid)
            : new(_map.GetMap(fromMap.MapId), fromMap.Position);

        var userVelocity = _physics.GetMapLinearVelocity(spawnCoords);

        var projectile = PredictedSpawnAtPosition(toSpawn, spawnCoords);
        var direction = _transform.ToMapCoordinates(toCoords).Position -
                        _transform.ToMapCoordinates(spawnCoords).Position;
        _gun.ShootProjectile(projectile, direction, userVelocity, performer, performer, speed);

        // ADT: no SetTarget, use HomingProjectile instead
        if (target != null && TryComp(projectile, out Content.Shared.ADT.Heretic.Common.HomingProjectileComponent? homing))
        {
            homing.Target = target.Value;
            Dirty(projectile, homing);
        }

        return projectile;
    }

    /// <summary>
    /// Heals everything imaginable
    /// </summary>
    /// <remarks>
    /// ADT: no shitmed, heals damage/blood via vanilla systems. bone/pain/wound params unused.
    /// </remarks>
    /// <param name="uid">Entity to heal</param>
    /// <param name="toHeal">how much to heal, null = full heal</param>
    /// <param name="boneHeal">unused, no shitmed</param>
    /// <param name="painHeal">unused, no shitmed</param>
    /// <param name="woundHeal">unused, no shitmed</param>
    /// <param name="bloodHeal">how much to restore blood, null = fully restore</param>
    /// <param name="bleedHeal">how much to heal bleeding, null = full heal</param>
    public void IHateWoundMed(Entity<DamageableComponent?, BodyComponent?> uid,
        DamageSpecifier? toHeal,
        FixedPoint2? boneHeal,
        FixedPoint2? painHeal,
        FixedPoint2? woundHeal,
        FixedPoint2? bloodHeal,
        FixedPoint2? bleedHeal)
    {
        if (!Resolve(uid, ref uid.Comp1, false))
            return;

        if (toHeal != null)
        {
            _dmg.TryChangeDamage(uid,
                toHeal,
                true,
                false,
                origin: uid);
        }
        else
        {
            TryComp<MobThresholdsComponent>(uid, out var thresholds);
            // do this so that the state changes when we set the damage
            _mobThreshold.SetAllowRevives(uid, true, thresholds);
            _dmg.SetAllDamage(uid, FixedPoint2.Zero);
            _mobThreshold.SetAllowRevives(uid, false, thresholds);
        }

        if (bleedHeal == FixedPoint2.Zero && bloodHeal == FixedPoint2.Zero ||
            !TryComp(uid, out BloodstreamComponent? blood))
            return;

        if (bleedHeal != FixedPoint2.Zero && blood.BleedAmount > 0f)
        {
            if (bleedHeal == null)
                _blood.TryModifyBleedAmount((uid, blood), -blood.BleedAmount);
            else
                _blood.TryModifyBleedAmount((uid, blood), bleedHeal.Value.Float());
        }

        if (bloodHeal == FixedPoint2.Zero || !TryComp(uid, out SolutionContainerManagerComponent? sol) ||
            !_solution.ResolveSolution((uid, sol), blood.BloodSolutionName, ref blood.BloodSolution))
            return;

        // ADT: BloodMaxVolume replaced by reference-solution volume * modifier
        var maxVolume = blood.BloodReferenceSolution.Volume * blood.MaxVolumeModifier;
        var curVolume = blood.BloodSolution.Value.Comp.Solution.Volume;
        if (curVolume >= maxVolume)
            return;

        _blood.TryModifyBloodLevel((uid, blood),
            bloodHeal == null ? maxVolume - curVolume : FixedPoint2.Min(bloodHeal.Value, maxVolume - curVolume));
    }

    public virtual void InvokeTouchSpell<T>(Entity<T> ent, EntityUid user) where T : Component, ITouchSpell
    {
        _audio.PlayPredicted(ent.Comp.Sound, user, user);
    }

    protected virtual void SpeakAbility(EntityUid ent, HereticActionComponent args) { }
}
