using Content.Shared.ActionBlocker;
using Content.Shared.ADT.Salvage.Components;
//ADT-Tweak-Start
//using Content.Shared.Buckle.Components;
using Content.Shared.Mind.Components;
//ADT-Tweak-End
using Content.Shared.Movement.Events;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Network;
//ADT-Tweak-Start
//using Robust.Shared.Audio;
//using Robust.Shared.Audio.Systems;
//ADT-Tweak-End
//using Robust.Shared.Physics.Components; ADT-Tweak
using Robust.Shared.Timing;

namespace Content.Shared.Chasm;

/// <summary>
///     Handles making entities fall into chasms when stepped on.
/// </summary>
public sealed class ChasmSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedGrapplingGunSystem _grapple = default!;
    //ADT-Tweak-Start
    //[Dependency] private readonly SharedAudioSystem _audio = default!;
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
    }

    private void OnStepTriggered(EntityUid uid, ChasmComponent component, ref StepTriggeredOffEvent args)
    {
        // already doomed
        if (HasComp<ChasmFallingComponent>(args.Tripper))
            return;

        StartFalling(uid, component, args.Tripper);
    }

    public void StartFalling(EntityUid chasm, ChasmComponent component, EntityUid tripper) //ADT-Tweak
    {
        var falling = AddComp<ChasmFallingComponent>(tripper);

        falling.NextDeletionTime = _timing.CurTime + falling.DeletionTime;
        _blocker.UpdateCanMove(tripper);

        //ADT-Tweak-Start
        //if (playSound)
        //    _audio.PlayPredicted(component.FallingSound, chasm, tripper);
        //ADT-Tweak-End
    }

    private void OnStepTriggerAttempt(EntityUid uid, ChasmComponent component, ref StepTriggerAttemptEvent args)
    {
        if (_grapple.IsEntityHooked(args.Tripper))
        {
            args.Cancelled = true;
            return;
        }

        args.Continue = true;
    }

    private void OnUpdateCanMove(EntityUid uid, ChasmFallingComponent component, UpdateCanMoveEvent args)
    {
        args.Cancel();
    }
}
