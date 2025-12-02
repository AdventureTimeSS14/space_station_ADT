using Content.Shared.ActionBlocker;
using Content.Shared.ADT.Salvage.Components;
//ADT-Tweak-Start
//using Content.Shared.Buckle.Components;
using Content.Shared.Mind.Components;
//ADT-Tweak-End
using Content.Shared.Movement.Events;
using Content.Shared.StepTrigger.Systems;
//ADT-Tweak-Start
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
//ADT-Tweak-End
using Robust.Shared.Network;
//using Robust.Shared.Physics.Components; ADT-Tweak
using Robust.Shared.Timing;
//ADT-Tweak-Start
using Content.Shared.ADT.Chasm;
//ADT-Tweak-End

namespace Content.Shared.Chasm;

/// <summary>
///     Handles making entities fall into chasms when stepped on.
/// </summary>
public sealed class ChasmSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly INetManager _net = default!;
    //ADT-Tweak-Start
    //[Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    //ADT-Tweak-End

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChasmComponent, StepTriggeredOffEvent>(OnStepTriggered);
        SubscribeLocalEvent<ChasmComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<ChasmFallingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // don't predict queuedels on client
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<ChasmFallingComponent>();
        while (query.MoveNext(out var uid, out var chasm))
        {
            if (_timing.CurTime < chasm.NextDeletionTime)
                continue;

            // ADT Jaunter start
            var ev = new BeforeChasmFallingEvent(uid);
            RaiseLocalEvent(uid, ref ev);
            if (ev.Cancelled)
            {
                RemComp<ChasmFallingComponent>(uid);
                _blocker.UpdateCanMove(uid);
                continue;
            }
            // ADT Jaunter end
            QueueDel(uid);
        }

        //ADT-Tweak-Start
        var pendingQuery = EntityQueryEnumerator<ChasmPendingFallComponent>();
        while (pendingQuery.MoveNext(out var tripper, out var pending))
        {
            if (_timing.CurTime < pending.NextFallTime)
                continue;

            if (!IsStillOnChasm(tripper, pending.ChasmUid))
            {
                RemComp<ChasmPendingFallComponent>(tripper);
                continue;
            }

            if (TryComp<ChasmComponent>(pending.ChasmUid, out var chasmComp) && !Deleted(pending.ChasmUid))
            {
                StartFalling(pending.ChasmUid, chasmComp, tripper);
            }

            RemComp<ChasmPendingFallComponent>(tripper);
        }
        //ADT-Tweak-End
    }

    private void OnStepTriggered(EntityUid uid, ChasmComponent component, ref StepTriggeredOffEvent args)
    {
        // already doomed
        if (HasComp<ChasmFallingComponent>(args.Tripper))
            return;

        //ADT-Tweak-Start
        var tripper = args.Tripper;

        if (HasComp<MindContainerComponent>(tripper))
        {
            if (TryComp<ChasmPendingFallComponent>(tripper, out var existingPending))
            {
                if (existingPending.ChasmUid != uid)
                {
                    StartFalling(uid, component, tripper);
                    RemComp<ChasmPendingFallComponent>(tripper);
                }
                else
                {
                    existingPending.NextFallTime = _timing.CurTime + TimeSpan.FromSeconds(1);
                }
            }
            else
            {
                var pending = AddComp<ChasmPendingFallComponent>(tripper);
                pending.NextFallTime = _timing.CurTime + TimeSpan.FromSeconds(1);
                pending.ChasmUid = uid;
            }
        }
        else
        {
            StartFalling(uid, component, tripper);
        }
        //ADT-Tweak-End
    }

    public void StartFalling(EntityUid chasm, ChasmComponent component, EntityUid tripper)
    {
        var falling = AddComp<ChasmFallingComponent>(tripper);

        falling.NextDeletionTime = _timing.CurTime + falling.DeletionTime;
        _blocker.UpdateCanMove(tripper);
    }

    private void OnStepTriggerAttempt(EntityUid uid, ChasmComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void OnUpdateCanMove(EntityUid uid, ChasmFallingComponent component, UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    //ADT-Tweak-Start
    private bool IsStillOnChasm(EntityUid tripper, EntityUid chasmUid)
    {
        if (Deleted(chasmUid))
            return false;

        var tripperCoords = _xform.GetMapCoordinates(tripper);
        var chasmCoords = _xform.GetMapCoordinates(chasmUid);

        if (tripperCoords.MapId != chasmCoords.MapId)
            return false;

        var distance = (tripperCoords.Position - chasmCoords.Position).Length();
        return distance < 0.5f; // Assuming tile-based positioning with threshold for overlap
    }
    //ADT-Tweak-End
}
