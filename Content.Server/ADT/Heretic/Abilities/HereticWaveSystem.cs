using System.Linq;
using Content.Shared.ADT.Heretic.Abilities;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
namespace Content.Server.ADT.Heretic.Abilities;
public sealed partial class HereticWaveSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EventHereticWaveOfDesperation>(OnWave);
    }
    private void OnWave(EventHereticWaveOfDesperation args)
    {
        if (args.Handled)
            return;
        var performer = args.Performer;
        if (!_heretic.TryGetHereticComponent(performer, out _, out _))
            return;
        if (!TryComp<CuffableComponent>(performer, out var cuffs) || cuffs.CuffedHandCount == 0)
            return;
        args.Handled = true;
        if (TryComp<HandsComponent>(performer, out var hands))
        {
            var cuffsToDel = _hands.EnumerateHeld((performer, hands)).ToList();
            foreach (var del in cuffsToDel)
            {
                if (HasComp<HandcuffComponent>(del))
                    QueueDel(del);
            }
        }
        var performerCoords = _xform.GetMapCoordinates(performer);
        foreach (var (target, _) in _lookup.GetEntitiesInRange<MobStateComponent>(Transform(performer).Coordinates, 3f))
        {
            if (target == performer)
                continue;
            if (!_mobState.IsAlive(target))
                continue;
            _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(3));
            var targetCoords = _xform.GetMapCoordinates(target);
            var dir = targetCoords.Position - performerCoords.Position;
            if (dir.LengthSquared() < 0.01f)
                dir = new System.Numerics.Vector2(1f, 0f);
            var dest = Transform(target).Coordinates.Offset(dir.Normalized() * 5f);
            _throw.TryThrow(target, dest, 10f, performer);
        }
    }
}
