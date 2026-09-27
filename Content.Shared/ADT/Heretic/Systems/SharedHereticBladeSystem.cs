//


using Content.Shared.Damage.Components;
using System.Linq;
using System.Numerics;
using System.Text;
using Content.Shared.ADT.Heretic.Common;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Teleportation;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Content.Shared.Popups;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Heretic.Systems;

public abstract class SharedHereticBladeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedHereticCombatMarkSystem _combatMark = default!;
    [Dependency] private readonly SharedRottingSystem _rotting = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly CosmosComboSystem _combo = default!;
    [Dependency] private readonly SharedStarMarkSystem _starMark = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedVoidCurseSystem _voidCurse = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    private const float BleedHealPerLivingHit = 0.5f;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticBladeComponent, UseInHandEvent>(OnInteract);
        SubscribeLocalEvent<HereticBladeComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HereticBladeComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<HereticBladeComponent, GetLightAttackRangeEvent>(OnGetRange);
        SubscribeLocalEvent<HereticBladeComponent, LightAttackSpecialInteractionEvent>(OnSpecial);
        SubscribeLocalEvent<HereticBladeComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnGetRange(Entity<HereticBladeComponent> ent, ref GetLightAttackRangeEvent args)
    {
        if (args.Target == null)
            return;

        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out _))
            return;

        if (ent.Comp.Path != heretic.CurrentPath || heretic.PathStage < 7)
            return;

        // Required for seeking blade, client weapon code should send attack event regardless of distance
        if (heretic.CurrentPath == "Void")
        {
            if (_net.IsServer)
                return;

            args.Range = 16f;
            args.Cancel = true;
            return;
        }

        if (heretic.CurrentPath != "Cosmos")
            return;

        if (HasComp<StarMarkComponent>(args.Target.Value))
        {
            if (heretic.Ascended)
            {
                args.Range = Math.Max(args.Range, 3.5f);
                return;
            }

            args.Range = Math.Max(args.Range, 2.5f);
        }

        var netEnt = GetNetEntity(args.User);
        var id = SharedStarTouchSystem.StarTouchBeamDataId;

        if (TryComp(args.Target.Value, out ComplexJointVisualsComponent? joint) &&
            joint.Data.Any(kvp => kvp.Key == netEnt && kvp.Value.Id == id))
            args.Range = Math.Max(args.Range, 3.5f);
    }

    // Void seeking blade

    private void OnSpecial(Entity<HereticBladeComponent> ent, ref LightAttackSpecialInteractionEvent args)
    {
        if (args.Target == null)
            return;

        if (SeekingBladeTeleport(ent, args.User, args.Target.Value, args.Range))
            args.Cancel = true;
    }

    private void OnAfterInteract(Entity<HereticBladeComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        if (SeekingBladeTeleport(ent, args.User, args.Target.Value))
            args.Handled = true;
    }

    private bool SeekingBladeTeleport(Entity<HereticBladeComponent> ent,
        EntityUid user,
        EntityUid target,
        float minRange = 0f,
        float maxRange = 16f)
    {
        var ev = new TeleportAttemptEvent();
        RaiseLocalEvent(user, ref ev);
        if (ev.Cancelled)
            return false;

        if (target == user || ent.Comp.Path != "Void" ||
            !_heretic.TryGetHereticComponent(user, out var heretic, out _) ||
            !TryComp(user, out CombatModeComponent? combat) ||
            heretic is not { CurrentPath: "Void", PathStage: >= 7 } || !HasComp<MobStateComponent>(target) ||
            !TryComp(ent, out MeleeWeaponComponent? melee) || melee.NextAttack > _timing.CurTime)
            return false;

        var xform = Transform(user);
        var targetXform = Transform(target);

        if (xform.MapID != targetXform.MapID)
            return false;

        var coords = _xform.GetWorldPosition(xform);
        var targetCoords = _xform.GetWorldPosition(targetXform);

        var dir = targetCoords - coords;
        var len = dir.Length();
        if (len >= maxRange || len <= minRange)
            return false;

        var normalized = new Vector2(dir.X / len, dir.Y / len);
        var ray = new CollisionRay(coords,
            normalized,
            (int) (CollisionGroup.Impassable | CollisionGroup.InteractImpassable));
        var result = _physics.IntersectRay(xform.MapID, ray, len, user).FirstOrNull();
        if (result != null && result.Value.HitEntity != target)
            return false;

        var newPos = result?.HitPos ?? targetCoords - normalized * 0.5f;

        _audio.PlayPredicted(ent.Comp.DepartureSound, xform.Coordinates, user);
        _xform.SetWorldPosition(user, newPos);
        var combatMode = _combat.IsInCombatMode(user, combat);
        _combat.SetInCombatMode(user, true, combat);
        if (!_melee.AttemptLightAttack(user, ent.Owner, melee, target))
            melee.NextAttack = _timing.CurTime + TimeSpan.FromSeconds(1f / _melee.GetAttackRate(ent, user, melee));
        melee.NextAttack += TimeSpan.FromSeconds(0.5);
        Dirty(ent.Owner, melee);
        _combat.SetInCombatMode(user, combatMode, combat);
        _audio.PlayPredicted(ent.Comp.ArrivalSound, xform.Coordinates, user);
        return true;
    }

    public void ApplySpecialEffect(EntityUid performer, EntityUid target, MeleeHitEvent args)
    {
        var path = HasComp<HereticBladeUserBonusDamageComponent>(performer) ? "Flesh" : null;
        if (_heretic.TryGetHereticComponent(performer, out var hereticComp, out _))
            path = hereticComp.CurrentPath;

        if (path == null)
            return;

        switch (path)
        {
            case "Ash":
                ApplyAshBladeEffect(target);
                break;

            case "Blade":
                // check event handler
                break;

            case "Flesh":
                // ultra bleed
                ApplyFleshBladeEffect(target);
                break;

            case "Lock":
                break;

            case "Void":
                _voidCurse.DoCurse(target);
                break;

            case "Rust":
                if (_mobState.IsDead(target))
                    _rotting.ReduceAccumulator(target, -TimeSpan.FromMinutes(1f));
                // ADT: no Goob Disgust/SecondSkin, skip living-target effect
                break;

            default:
                return;
        }
    }

    private void OnInteract(Entity<HereticBladeComponent> ent, ref UseInHandEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out _))
            return;

        if (heretic.Ascended)
        {
            _popup.PopupClient(Loc.GetString("heretic-blade-break-fail-acended-message"), args.User, args.User);
            return;
        }

        if (!HasRandomTeleport(ent))
            return;

        var ev = new TeleportAttemptEvent();
        RaiseLocalEvent(args.User, ref ev);
        if (ev.Cancelled)
            return;

        RandomTeleport(args.User, ent);
        _audio.PlayPredicted(ent.Comp.ShatterSound, args.User, args.User);
        _popup.PopupClient(Loc.GetString("heretic-blade-use"), args.User, args.User);
        args.Handled = true;
    }

    private void OnExamine(Entity<HereticBladeComponent> ent, ref ExaminedEvent args)
    {
        if (!HasRandomTeleport(ent))
            return;

        if (!_heretic.TryGetHereticComponent(args.Examiner, out var heretic, out _) || heretic.Ascended)
            return;

        args.PushMarkup(Loc.GetString("heretic-blade-examine"));
    }

    private void OnMeleeHit(Entity<HereticBladeComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || string.IsNullOrWhiteSpace(ent.Comp.Path))
            return;

        _heretic.TryGetHereticComponent(args.User, out var hereticComp, out _);

        if (TryComp(args.User, out HereticBladeUserBonusDamageComponent? bonus) &&
            (bonus.Path == null || bonus.Path == ent.Comp.Path))
        {
            args.BonusDamage += args.BaseDamage * bonus.BonusMultiplier; // "ghouls can use bloody blades effectively... so real..."
            if (hereticComp == null)
            {
                foreach (var hit in args.HitEntities)
                {
                    ApplySpecialEffect(args.User, hit, args);
                }
            }
        }

        if (hereticComp == null)
            return;

        if (ent.Comp.Path != hereticComp.CurrentPath)
            return;

        if (hereticComp.PathStage >= 7)
        {
            switch (hereticComp.CurrentPath)
            {
                case "Rust":
                    args.BonusDamage += new DamageSpecifier
                    {
                        DamageDict =
                        {
                            { "Poison", 5f },
                        },
                    };
                    break;
                case "Blade":
                    args.BonusDamage += new DamageSpecifier
                    {
                        DamageDict =
                        {
                            { "Structural", 10f },
                        },
                    };
                    break;
                case "Cosmos":
                    args.BonusDamage += new DamageSpecifier
                    {
                        DamageDict =
                        {
                            { "Heat", 5f },
                        },
                    };

                    var hitEnts = args.HitEntities;

                    if (hitEnts.Count == 0)
                        break;

                    _combo.ComboProgress(args.User, hereticComp, hitEnts);

                    foreach (var uid in hitEnts)
                    {
                        _starMark.TryApplyStarMark(uid);
                    }
                    break;
            }
        }

        var aliveMobsCount = 0;

        foreach (var hit in args.HitEntities)
        {
            if (hit == args.User)
                continue;

            if (TryComp(hit, out MobStateComponent? mobState) && mobState.CurrentState != MobState.Dead)
                aliveMobsCount++;

            if (TryComp<HereticCombatMarkComponent>(hit, out var mark))
                _combatMark.ApplyMarkEffect(hit, mark, ent.Comp.Path, args.User, hereticComp);

            if (hereticComp.PathStage >= 7)
                ApplySpecialEffect(args.User, hit, args);
        }

        // blade path exclusive.
        if (HasComp<SilverMaelstromComponent>(args.User))
        {
            args.BonusDamage += args.BaseDamage * 0.5f;
            if (aliveMobsCount > 0)
            {
                var baseHeal = args.BaseDamage.GetTotal();
                var bonusHeal = HasComp<MansusInfusedComponent>(ent) ? baseHeal : baseHeal / 2f;
                bonusHeal *= aliveMobsCount;

                if (TryComp<DamageableComponent>(args.User, out var dmg))
                    SanguineLifeSteal(args.User, bonusHeal, dmg);

                // ADT: vampirism weakly mends bleeding as well
                if (_net.IsServer &&
                    TryComp<BloodstreamComponent>(args.User, out var blood) && blood.BleedAmount > 0f)
                    _bloodstream.TryModifyBleedAmount((args.User, blood), -BleedHealPerLivingHit * aliveMobsCount);
            }
        }
    }

    // ADT: replaces Goob's SanguineStrikeSystem.LifeSteal, no shitmed
    private void SanguineLifeSteal(EntityUid uid, FixedPoint2 amount, DamageableComponent damageable)
    {
        var totalUserDamage = _damageable.GetTotalDamage((uid, damageable));
        if (totalUserDamage <= FixedPoint2.Zero)
            return;

        // ADT: HealEvenly splits healing proportionally by damage type
        _damageable.HealEvenly((uid, damageable), -FixedPoint2.Min(amount, totalUserDamage));
    }

    protected virtual void ApplyAshBladeEffect(EntityUid target) { }

    protected virtual void ApplyFleshBladeEffect(EntityUid target) { }

    // ADT: RandomTeleportComponent is server-only, access via override
    protected virtual bool HasRandomTeleport(EntityUid blade) => false;

    protected virtual void RandomTeleport(EntityUid user, EntityUid blade) { }
}
