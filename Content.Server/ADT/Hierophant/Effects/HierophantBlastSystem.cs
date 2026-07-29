using Content.Shared.ADT.Hierophant;
using Content.Shared.ADT.Hierophant.Effects;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Mech.Components;
using Content.Shared.Mining.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Hierophant.Effects;

public sealed class HierophantBlastSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float TileRadius = 0.45f;

    private readonly HashSet<EntityUid> _hitBuffer = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HierophantBlastComponent, MapInitEvent>(OnBlastInit);
    }

    private void OnBlastInit(Entity<HierophantBlastComponent> ent, ref MapInitEvent args)
    {
        var now = _timing.CurTime;
        ent.Comp.DetonateAt = now + ent.Comp.DetonateDelay;
        ent.Comp.BurstEndAt = ent.Comp.DetonateAt + ent.Comp.BurstDuration;

        _audio.PlayPvs(ent.Comp.SpawnSound, ent.Owner, AudioParams.Default.WithVolume(-5f));

        DrillRock(ent.Owner); // TODO rework with GatherableSystem idk?
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<HierophantBlastComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.DetonateAt)
                continue;

            if (now > comp.BurstEndAt)
            {
                comp.Bursting = false;
                continue;
            }

            comp.Bursting = true;
            DoDamage((uid, comp));
        }
    }

    private void DoDamage(Entity<HierophantBlastComponent> ent)
    {
        if (ent.Comp.Damage <= 0f)
            return;

        var coords = _transform.GetMapCoordinates(ent.Owner);

        _hitBuffer.Clear();
        _lookup.GetEntitiesInRange(coords.MapId, coords.Position, TileRadius, _hitBuffer);

        foreach (var target in _hitBuffer)
        {
            if (!ent.Comp.HitThings.Add(target))
                continue;

            if (target == ent.Comp.Caster)
                continue;

            if (HasComp<MobStateComponent>(target))
            {
                DamageLiving(ent, target);
                continue;
            }

            if (HasComp<MechComponent>(target))
                DamageMech(ent, target);
        }
    }

    private void DamageLiving(Entity<HierophantBlastComponent> ent, EntityUid target)
    {
        if (_mobState.IsDead(target))
            return;

        if (ent.Comp.FriendlyFireCheck
            && ent.Comp.Caster != null
            && _npcFaction.IsEntityFriendly(ent.Comp.Caster.Value, target))
            return;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Heat", ent.Comp.Damage); // todo move all damage groups into component's

        if (ent.Comp.MonsterDamageBoost && IsMonster(target))
            damage.DamageDict["Heat"] += ent.Comp.Damage;

        _damageable.TryChangeDamage(target, damage, origin: ent.Comp.Caster);

        _color.RaiseEffect(ent.Comp.FlashColor, new List<EntityUid> { target }, Filter.Pvs(target, entityManager: EntityManager));
        _audio.PlayPvs(ent.Comp.HitSound, target, AudioParams.Default.WithVolume(-4f));
    }

    private void DamageMech(Entity<HierophantBlastComponent> ent, EntityUid target)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Heat", ent.Comp.Damage);

        _damageable.TryChangeDamage(target, damage, origin: ent.Comp.Caster);
        _audio.PlayPvs(ent.Comp.HitSound, target, AudioParams.Default.WithVolume(-4f));
    }

    private bool IsMonster(EntityUid target)
    {
        if (HasComp<HierophantComponent>(target))
            return true;

        return HasComp<NpcFactionMemberComponent>(target) && !HasComp<ActorComponent>(target) && HasComp<FaunaComponent>(target);
    }

    private void DrillRock(EntityUid blast)
    {
        var coords = _transform.GetMapCoordinates(blast);

        _hitBuffer.Clear();
        _lookup.GetEntitiesInRange(coords.MapId, coords.Position, TileRadius, _hitBuffer);

        foreach (var candidate in _hitBuffer)
        {
            if (!HasComp<OreVeinComponent>(candidate))
                continue;

            QueueDel(candidate);
        }
    }
}
