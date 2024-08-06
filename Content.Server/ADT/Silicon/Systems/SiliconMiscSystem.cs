using Content.Shared.ADT.Silicon.Components;
using Content.Shared.Bed.Sleep;
using static Content.Shared.Repairable.SharedRepairableSystem;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;

namespace Content.Server.ADT.Silicon.Sytstems;

public sealed class SiliconMiscSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem _blood = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconComponent, TryingToSleepEvent>(OnTryingToSleep);
        //SubscribeLocalEvent<SiliconComponent, RepairFinishedEvent>(OnRepairFinished);
    }

    /// <summary>
    ///     Stops Silicons from being capable of sleeping.
    /// </summary>
    /// <remarks>
    ///     This is stupid.
    /// </remarks>
    private void OnTryingToSleep(EntityUid uid, SiliconComponent component, ref TryingToSleepEvent args)
    {
        args.Cancelled = true;
    }

    /// <summary>
    ///     Ensure Silicons stop bleeding when repaired, if they can bleed.
    /// </summary>
    //private void OnRepairFinished(EntityUid uid, SiliconComponent component, RepairFinishedEvent args)
    //{
    //    if (TryComp<BloodstreamComponent>(uid, out var bloodComp))
    //    {
    //        _blood.TryModifyBleedAmount(uid, -bloodComp.BleedAmount, bloodComp);
    //    }
    //}

}
