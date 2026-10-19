using System.Numerics;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC;

public sealed class ADTNpcCombatMemorySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTNpcCombatMemoryComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<ADTNpcCombatMemoryComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<ADTNpcCombatMemoryComponent, EntitySpokeEvent>(OnSpoke);
    }

    private void OnSpoke(EntityUid uid, ADTNpcCombatMemoryComponent component, EntitySpokeEvent args)
    {
        component.Spoke = true;
        component.LastSpoke = _timing.CurTime;
    }

    private void OnShotAttempted(EntityUid uid, ADTNpcCombatMemoryComponent component, ref ShotAttemptedEvent args)
    {
        if (args.Cancelled || !HasComp<ActiveNPCComponent>(uid))
            return;

        if (args.Used.Comp.ShootCoordinates is not { } coordinates)
            return;

        var from = _transform.GetMapCoordinates(uid);
        var to = _transform.ToMapCoordinates(coordinates);

        if (from.MapId != to.MapId)
            return;

        var segment = to.Position - from.Position;
        var lengthSq = segment.LengthSquared();

        if (lengthSq < 0.01f)
            return;

        foreach (var found in _lookup.GetEntitiesInRange<NpcFactionMemberComponent>(from, MathF.Sqrt(lengthSq) + 0.5f))
        {
            var ally = found.Owner;

            if (ally == uid || _mobState.IsIncapacitated(ally) || !_faction.IsEntityFriendly(uid, ally))
                continue;

            var pos = _transform.GetMapCoordinates(ally).Position;
            var along = Vector2.Dot(pos - from.Position, segment) / lengthSq;

            if (along <= 0f || along >= 1f)
                continue;

            if (Vector2.Distance(from.Position + segment * along, pos) > component.AllyClearance)
                continue;

            args.Cancel();
            component.Blocked = true;
            component.LastBlocked = _timing.CurTime;
            return;
        }
    }

    private void OnDamageDealt(EntityUid uid, ADTNpcCombatMemoryComponent component, ref DamageDealtEvent args)
    {
        if (!args.Damage.AnyPositive() || args.Origin == null || args.Origin == uid)
            return;

        component.Hurt = true;
        component.LastHurt = _timing.CurTime;
    }
}
