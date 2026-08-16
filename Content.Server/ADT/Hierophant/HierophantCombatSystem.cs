using System.Diagnostics.CodeAnalysis;
using Content.Server.ADT.Salvage.Systems;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.ADT.Hierophant;
using Content.Shared.ADT.Hierophant.Effects;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Hierophant;

public enum HierophantMajorAttack : byte
{
    BlinkSpam,
    CrossBlastSpam,
    ChaserSwarm,
}

public sealed class HierophantCombatSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly HierophantAttacksSystem _attacks = default!;
    [Dependency] private readonly HierophantSystem _hierophant = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MegafaunaSystem _megafauna = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float BaselineSlowness = 1.5f;

    private const float BaselineSprintSpeed = 4.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HierophantComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<HierophantComponent, HierophantBlinkActionEvent>(OnBlinkAction);
        SubscribeLocalEvent<HierophantComponent, HierophantChaserSwarmActionEvent>(OnChaserSwarmAction);
        SubscribeLocalEvent<HierophantComponent, HierophantCrossBlastsActionEvent>(OnCrossBlastsAction);
        SubscribeLocalEvent<HierophantComponent, HierophantBlinkSpamActionEvent>(OnBlinkSpamAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<HierophantComponent, HTNComponent>();

        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            if (HasComp<ActorComponent>(uid))
                continue;

            if (!_npc.IsAwake(uid, htn))
                continue;

            if (_mobState.IsDead(uid))
                continue;

            if (comp.Blinking)
                continue;

            if (now < comp.NextRangedAt)
                continue;

            if (!_hierophant.TryGetTarget(uid, out var target) || target == null)
            {
                comp.LastTarget = null;
                continue;
            }

            if (!TargetOnSameGrid(uid, target.Value))
            {
                if (TryComp<HTNComponent>(uid, out var targetHtn))
                    targetHtn.Blackboard.Remove<EntityUid>("Target");

                comp.LastTarget = null;
                continue;
            }

            if (comp.LastTarget != target)
            {
                comp.LastTarget = target;
                _megafauna.Say(uid, "adt-hierophant-target", 3);
            }

            OpenFire((uid, comp), target.Value);
        }
    }

    private bool TargetOnSameGrid(EntityUid ent, EntityUid target)
    {
        var ourGrid = _transform.GetGrid(ent);
        var targetGrid = _transform.GetGrid(target);

        return ourGrid == targetGrid;
    }

    private void OpenFire(Entity<HierophantComponent> ent, EntityUid target)
    {
        _hierophant.CalculateRage(ent);

        var anger = ent.Comp.AngerModifier;
        var blinkCounter = 1 + (int) MathF.Round(anger * 0.08f);
        var crossCounter = 1 + (int) MathF.Round(anger * 0.12f);

        _attacks.ArenaTrap(ent, target);

        ent.Comp.NextRangedAt = _timing.CurTime +
            TimeSpan.FromSeconds(Math.Max(0.5, ent.Comp.RangedCooldownTime.TotalSeconds - anger * 0.075));

        var targetSlowness = GetTargetSlowness(ent, target);
        ent.Comp.ChaserSpeed = MathF.Max(0.1f, (3f - anger * 0.04f + (targetSlowness - 1f) * 0.5f) * 0.1f);

        var ourCoords = _transform.GetMapCoordinates(ent.Owner);
        var targetCoords = _transform.GetMapCoordinates(target);
        var distance = HierophantAttacksSystem.ChebyshevDistance(ourCoords, targetCoords);

        if (_mobState.IsDead(target) && distance > 2)
        {
            _attacks.Blink(ent, targetCoords);
            return;
        }

        var belowHalfHealth = IsBelowHalfHealth(ent);

        if (_random.Prob(Math.Clamp(anger * 0.0075f, 0f, 1f)))
        {
            var possibilities = new List<HierophantMajorAttack>();

            if (crossCounter > 1)
                possibilities.Add(HierophantMajorAttack.CrossBlastSpam);

            if (distance > 2)
                possibilities.Add(HierophantMajorAttack.BlinkSpam);

            if (_timing.CurTime > ent.Comp.NextChaserAt)
            {
                if (_random.Prob(Math.Clamp(anger * 0.02f, 0f, 1f)))
                    possibilities = new List<HierophantMajorAttack> { HierophantMajorAttack.ChaserSwarm };
                else
                    possibilities.Add(HierophantMajorAttack.ChaserSwarm);
            }

            if (possibilities.Count > 0)
            {
                var picked = _random.Pick(possibilities);

                switch (picked)
                {
                    case HierophantMajorAttack.BlinkSpam:
                        _attacks.BlinkSpam(ent, target, blinkCounter, targetSlowness, belowHalfHealth);
                        break;

                    case HierophantMajorAttack.CrossBlastSpam:
                        _attacks.CrossBlastSpam(ent, target, crossCounter, targetSlowness);
                        break;

                    case HierophantMajorAttack.ChaserSwarm:
                        _attacks.ChaserSwarm(ent, target, targetSlowness);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(picked), picked, "Unhandled Hierophant major attack.");
                }

                return;
            }
        }

        if (_timing.CurTime > ent.Comp.NextChaserAt)
        {
            FireChaser(ent, target, anger, distance);
            return;
        }

        if (_random.Prob(Math.Clamp(0.1f + anger * 0.005f, 0f, 1f)) && distance > 2)
        {
            _attacks.Blink(ent, targetCoords);
            return;
        }

        if (_random.Prob(Math.Clamp(0.7f - anger * 0.01f, 0f, 1f)))
        {
            if (belowHalfHealth && _random.Prob(Math.Clamp(anger * 0.02f / targetSlowness, 0f, 1f)))
                _attacks.Blasts(ent, target, HierophantAttacksSystem.AllDirs);
            else if (_random.Prob(0.6f))
                _attacks.Blasts(ent, target, HierophantAttacksSystem.Cardinals);
            else
                _attacks.Blasts(ent, target, HierophantAttacksSystem.Diagonals);

            return;
        }

        _attacks.Burst(ent, _transform.GetMoverCoordinates(ent.Owner));
    }

    private void FireChaser(Entity<HierophantComponent> ent, EntityUid target, float anger, float distance)
    {
        ent.Comp.NextChaserAt = _timing.CurTime + ent.Comp.ChaserCooldownTime;

        SpawnChaser(ent, target, ent.Comp.ChaserSpeed, 0, null);

        if (!_random.Prob(Math.Clamp(anger * 0.01f, 0f, 1f)) && distance > 1)
            return;

        if (target == ent.Owner)
            return;

        var dir = _random.Pick(HierophantAttacksSystem.Cardinals);
        SpawnChaser(ent, target, ent.Comp.ChaserSpeed * 1.5f, 4, dir);
    }

    private void SpawnChaser(Entity<HierophantComponent> ent, EntityUid target, float speed, int moving, Direction? dir)
    {
        var chaser = Spawn(ent.Comp.ChaserProto, _transform.GetMoverCoordinates(ent.Owner));

        if (!TryComp<HierophantChaserComponent>(chaser, out var comp))
            return;

        comp.Caster = ent.Owner;
        comp.Target = target;
        comp.Speed = speed;

        if (moving > 0)
            comp.Moving = moving;

        if (dir != null)
            comp.MovingDir = dir.Value;
    }

    private void OnMeleeHit(Entity<HierophantComponent> ent, ref MeleeHitEvent args)
    {
        if (ent.Comp.Blinking)
            return;

        if (args.HitEntities.Count == 0)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!_mobState.IsDead(target))
                continue;

            Devour(ent, target);
            return;
        }

        var ourCoords = _transform.GetMoverCoordinates(ent.Owner);

        if (_timing.CurTime > ent.Comp.NextRangedAt)
        {
            _hierophant.CalculateRage(ent);
            ent.Comp.NextRangedAt = _timing.CurTime +
                TimeSpan.FromSeconds(Math.Max(0.5, ent.Comp.RangedCooldownTime.TotalSeconds - ent.Comp.AngerModifier * 0.075));
            _attacks.Burst(ent, ourCoords);
        }
        else
        {
            ent.Comp.BurstRange = 3;
            _attacks.Burst(ent, ourCoords, spreadSpeed: 0.25f);
        }
    }

    private void Devour(Entity<HierophantComponent> ent, EntityUid target)
    {
        _megafauna.Say(ent.Owner, "adt-hierophant-kill", 4);

        var healing = new DamageSpecifier();
        healing.DamageDict.Add("Heat", -100f);
        healing.DamageDict.Add("Blunt", -100f); // move into compoentn
        _damageable.TryChangeDamage(ent.Owner, healing, true);

        QueueDel(target);
    }

    private float GetTargetSlowness(Entity<HierophantComponent> ent, EntityUid target)
    {
        var slowness = BaselineSlowness;

        if (TryComp<MovementSpeedModifierComponent>(target, out var speed) && speed.CurrentSprintSpeed > 0f)
            slowness = BaselineSlowness * (BaselineSprintSpeed / speed.CurrentSprintSpeed);

        if (HasComp<ActorComponent>(ent.Owner))
            slowness += 1f;

        return Math.Clamp(slowness, 1f, 5f);
    }

    private bool IsBelowHalfHealth(Entity<HierophantComponent> ent)
    {
        if (!TryComp<MobThresholdsComponent>(ent, out var thresholds))
            return false;

        var maxHealth = 0f;
        foreach (var (damage, _) in thresholds.Thresholds)
        {
            if ((float) damage > maxHealth)
                maxHealth = (float) damage;
        }

        if (maxHealth <= 0f)
            return false;

        var taken = (float) _damageable.GetTotalDamage(ent.Owner);
        return taken >= maxHealth * 0.5f;
    }

    private void OnBlinkAction(Entity<HierophantComponent> ent, ref HierophantBlinkActionEvent args)
    {
        if (args.Handled)
            return;

        var coords = _transform.ToMapCoordinates(args.Target);

        if (coords.MapId == MapId.Nullspace)
            return;

        args.Handled = true;
        _attacks.Blink(ent, coords);
    }

    private void OnChaserSwarmAction(Entity<HierophantComponent> ent, ref HierophantChaserSwarmActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetActionTarget(args.Target, out var target))
            return;

        args.Handled = true;
        _hierophant.CalculateRage(ent);
        _attacks.ChaserSwarm(ent, target.Value, GetTargetSlowness(ent, target.Value));
    }

    private void OnCrossBlastsAction(Entity<HierophantComponent> ent, ref HierophantCrossBlastsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetActionTarget(args.Target, out var target))
            return;

        args.Handled = true;
        _hierophant.CalculateRage(ent);

        var crossCounter = 1 + (int) MathF.Round(ent.Comp.AngerModifier * 0.12f);
        _attacks.CrossBlastSpam(ent, target.Value, crossCounter, GetTargetSlowness(ent, target.Value));
    }

    private void OnBlinkSpamAction(Entity<HierophantComponent> ent, ref HierophantBlinkSpamActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetActionTarget(args.Target, out var target))
            return;

        args.Handled = true;
        _hierophant.CalculateRage(ent);

        var blinkCounter = 1 + (int) MathF.Round(ent.Comp.AngerModifier * 0.08f);
        _attacks.BlinkSpam(ent, target.Value, Math.Max(2, blinkCounter), GetTargetSlowness(ent, target.Value), true);
    }

    private bool TryGetActionTarget(EntityCoordinates coords, [NotNullWhen(true)] out EntityUid? target)
    {
        target = null;

        var mapCoords = _transform.ToMapCoordinates(coords);

        if (mapCoords.MapId == MapId.Nullspace)
            return false;

        var lookup = EntityManager.System<EntityLookupSystem>();
        var candidates = lookup.GetEntitiesInRange<MobStateComponent>(mapCoords, 0.75f);

        foreach (var candidate in candidates)
        {
            if (_mobState.IsDead(candidate.Owner))
                continue;

            target = candidate.Owner;
            return true;
        }

        return false;
    }
}
