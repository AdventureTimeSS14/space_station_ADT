using System.Numerics;
using Content.Server.NPC.HTN;
using Content.Server.Weapons.Melee;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using FixturesComponent = Robust.Shared.Physics.FixturesComponent;

namespace Content.Server.ADT.Salvage.Systems;

public sealed partial class MegafaunaSystem
{
    [Dependency] private MeleeWeaponSystem _melee = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    private const float SmashCardinalThreshold = 0.4f;

    private readonly Dictionary<EntityUid, Vector2> _smashTargets = new();

    private void UpdateSmash(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ADTMegafaunaAIComponent, HTNComponent, PhysicsComponent>();

        while (query.MoveNext(out var uid, out var ai, out var htn, out var physics))
        {
            if (!ai.SmashObstacles || now < ai.NextSmashAt)
                continue;

            ai.NextSmashAt = now + ai.SmashCooldown;

            if (HasComp<ActorComponent>(uid))
                continue;

            if (!_npc.IsAwake(uid, htn))
                continue;

            if (_mobState.IsDead(uid))
                continue;

            if (!TryGetTarget(uid, out var found) || found is not { } target)
                continue;

            if (IsAdjacent(uid, target, ai.AdjacentOnlyOnGrid))
                continue;

            TrySmashTowards((uid, ai), target, physics.CollisionMask);
        }
    }

    private void TrySmashTowards(Entity<ADTMegafaunaAIComponent> ent, EntityUid target, int mask)
    {
        var origin = _transform.GetMapCoordinates(ent.Owner);
        var theirs = _transform.GetMapCoordinates(target);

        if (origin.MapId == MapId.Nullspace || origin.MapId != theirs.MapId)
            return;

        var diff = theirs.Position - origin.Position;
        var distance = diff.Length();
        if (distance < 0.01f)
            return;

        var direction = diff / distance;

        var reach = GetBodyExtent(ent.Owner) + ent.Comp.SmashReach;

        _smashTargets.Clear();
        CollectSmashTargets(ent.Owner, origin, direction, mask, reach);

        if (Math.Abs(direction.X) >= SmashCardinalThreshold && Math.Abs(direction.Y) >= SmashCardinalThreshold)
        {
            CollectSmashTargets(ent.Owner, origin, new Vector2(Math.Sign(direction.X), 0f), mask, reach);
            CollectSmashTargets(ent.Owner, origin, new Vector2(0f, Math.Sign(direction.Y)), mask, reach);
        }

        if (_smashTargets.Count == 0)
            return;

        TryComp<MeleeWeaponComponent>(ent.Owner, out var melee);

        var lunged = false;
        foreach (var (obstacle, hitPos) in _smashTargets)
        {
            if (melee != null)
            {
                if (!lunged)
                {
                    DoSmashLunge(ent.Owner, hitPos, melee);
                    lunged = true;
                }

                _audio.PlayPvs(melee.HitSound, obstacle);
            }

            _damageable.TryChangeDamage(obstacle, ent.Comp.SmashDamage, origin: ent.Owner);
        }

        _smashTargets.Clear();
    }

    private void CollectSmashTargets(EntityUid uid, MapCoordinates origin, Vector2 direction, int mask, float reach)
    {
        var ray = new CollisionRay(origin.Position, direction, mask);

        foreach (var result in _physics.IntersectRay(origin.MapId, ray, reach, uid, false))
        {
            var hit = result.HitEntity;

            if (_smashTargets.ContainsKey(hit))
                continue;

            if (!TryComp<PhysicsComponent>(hit, out var body) || body.BodyType != BodyType.Static)
                continue;

            if (HasComp<MobStateComponent>(hit) || !HasComp<DamageableComponent>(hit))
                continue;

            if (IsWall(hit))
                continue;

            _smashTargets.Add(hit, result.HitPos);
        }
    }

    private float GetBodyExtent(EntityUid uid)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return 0f;

        var extent = 0f;
        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!fixture.Hard || fixture.CollisionMask == 0)
                continue;

            var box = fixture.Shape.ComputeAABB(Robust.Shared.Physics.Transform.Empty, 0);
            extent = Math.Max(extent, Math.Max(Math.Max(-box.Left, box.Right), Math.Max(-box.Bottom, box.Top)));
        }

        return extent;
    }

    private void DoSmashLunge(EntityUid uid, Vector2 hitPos, MeleeWeaponComponent melee)
    {
        var xform = Transform(uid);
        var localPos = Vector2.Transform(hitPos, _transform.GetInvWorldMatrix(xform));

        if (localPos.LengthSquared() <= 0f)
            return;

        localPos = xform.LocalRotation.RotateVec(localPos);
        _melee.DoLunge(uid, uid, melee.Angle, localPos, melee.Animation, false);
    }
}
