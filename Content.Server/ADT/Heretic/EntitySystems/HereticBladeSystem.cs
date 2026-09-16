using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Teleportation;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server.Heretic.EntitySystems;

public sealed class HereticBladeSystem : SharedHereticBladeSystem
{
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly TeleportSystem _teleport = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _sol = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    protected override void ApplyAshBladeEffect(EntityUid target)
    {
        base.ApplyAshBladeEffect(target);

        _flammable.AdjustFireStacks(target, 2.5f, null, true); // ADT: no fire resist piercing
    }

    protected override void ApplyFleshBladeEffect(EntityUid target)
    {
        base.ApplyFleshBladeEffect(target);

        if (!TryComp(target, out BloodstreamComponent? bloodStream))
            return;

        _blood.TryModifyBleedAmount((target, bloodStream), 2f);

        if (!_sol.ResolveSolution(target,
                bloodStream.BloodSolutionName,
                ref bloodStream.BloodSolution,
                out var bloodSolution))
            return;

        _puddle.TrySpillAt(target, bloodSolution.SplitSolution(10), out _);
    }

    protected override bool HasRandomTeleport(EntityUid blade)
    {
        return HasComp<RandomTeleportComponent>(blade);
    }

    protected override void RandomTeleport(EntityUid user, EntityUid blade)
    {
        base.RandomTeleport(user, blade);

        if (!TryComp(blade, out RandomTeleportComponent? comp))
            return;

        _teleport.RandomTeleport(user, comp, false);
        QueueDel(blade);
    }
}
