using System.Numerics;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.NPC;
using Content.Shared.ADT.Xenobiology.Components;
using Robust.Shared.Map;

namespace Content.Server.ADT.NPC;

public sealed class ADTNPCRetargetSystem : EntitySystem
{
    [Dependency] private readonly NPCSteeringSystem _steering = default!;

    private const string TargetKey = "Target";

    private const string TargetCoordinatesKey = "TargetCoordinates";

    private const float UpdateCooldown = 0.25f;

    private float _accumulator;

    private EntityQuery<SlimeComponent> _slimeQuery;

    public override void Initialize()
    {
        base.Initialize();

        _slimeQuery = GetEntityQuery<SlimeComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator -= frameTime;

        if (_accumulator > 0f)
            return;

        _accumulator = UpdateCooldown;

        var query = EntityQueryEnumerator<ActiveNPCComponent, HTNComponent, NPCSteeringComponent>();

        while (query.MoveNext(out var uid, out _, out var htn, out var steering))
        {
            if (!_slimeQuery.HasComp(uid))
                continue;

            if (!htn.Blackboard.TryGetValue<EntityUid>(TargetKey, out var target, EntityManager))
                continue;

            if (TerminatingOrDeleted(target))
                continue;

            if (steering.Status == SteeringStatus.NoPath)
                continue;

            // Always update coordinates to follow moving targets
            var coordinates = new EntityCoordinates(target, Vector2.Zero);
            _steering.Register(uid, coordinates, steering);
            htn.Blackboard.SetValue(TargetCoordinatesKey, coordinates);
        }
    }
}
