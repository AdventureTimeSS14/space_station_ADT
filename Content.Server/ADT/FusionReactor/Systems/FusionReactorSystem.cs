using Content.Server.FusionReactor.Components;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.FusionReactor.Systems;

public sealed class FusionCoreSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FusionCoreComponent>();
        while (query.MoveNext(out var uid, out var core))
        {
            if (!core.Active)
                continue;

            var temp = core.Temperature;
            var pressure = core.Pressure;

            if (temp > 1000f && pressure > 500000f) // 5 MPa
            {
                var deuterium = core.Gas.GetMoles(Gas.Deuterium);
                var tritium = core.Gas.GetMoles(Gas.Tritium);

                if (deuterium > 1f && tritium > 1f)
                {
                    var reactionRate = MathF.Min(deuterium, tritium);
                    var energyReleased = reactionRate * 5000f;

                    core.Gas.Temperature += energyReleased / 100f;
                    core.Gas.AdjustMoles(Gas.Deuterium, -reactionRate);
                    core.Gas.AdjustMoles(Gas.Tritium, -reactionRate);
                    core.Gas.AdjustMoles(Gas.Helium, reactionRate);
                    core.PowerOutput += energyReleased;
                }
            }
        }
    }
}


// FusionCoolerSystem.cs
using Content.Server.FusionReactor.Components;
using Content.Shared.Atmos;

namespace Content.Server.FusionReactor.Systems;

public sealed class FusionCoolerSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FusionCoolerComponent>();
        while (query.MoveNext(out var uid, out var cooler))
        {
            if (cooler.CoreUid is not { } coreUid || !TryComp(coreUid, out FusionCoreComponent? core))
                continue;

            var deltaTemp = core.Gas.Temperature - cooler.Gas.Temperature;
            var transferAmount = deltaTemp * cooler.HeatTransferEfficiency * frameTime;

            core.Gas.Temperature -= transferAmount;
            cooler.Gas.Temperature += transferAmount;
        }
    }
}


// FusionInletSystem.cs
using Content.Server.FusionReactor.Components;
using Content.Shared.Atmos;

namespace Content.Server.FusionReactor.Systems;

public sealed class FusionInletSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FusionInletComponent>();
        while (query.MoveNext(out var uid, out var inlet))
        {
            if (inlet.TargetCore is not { } coreUid || !TryComp(coreUid, out FusionCoreComponent? core))
                continue;

            var source = _atmosphereSystem.GetContainingMixture(uid, true);
            if (source == null)
                continue;

            var transfer = source.Remove(1f);
            core.Gas.Merge(transfer);
        }
    }
}


// FusionOutletSystem.cs
using Content.Server.FusionReactor.Components;
using Content.Shared.Atmos;

namespace Content.Server.FusionReactor.Systems;

public sealed class FusionOutletSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FusionOutletComponent>();
        while (query.MoveNext(out var uid, out var outlet))
        {
            if (outlet.SourceCore is not { } coreUid || !TryComp(coreUid, out FusionCoreComponent? core))
                continue;

            var target = _atmosphereSystem.GetContainingMixture(uid, true);
            if (target == null)
                continue;

            var removed = core.Gas.Remove(0.5f);
            target.Merge(removed);
        }
    }
}
