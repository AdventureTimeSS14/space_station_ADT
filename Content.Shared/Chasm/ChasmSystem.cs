using Content.Shared.ActionBlocker;
<<<<<<< ours
using Content.Shared.ADT.Salvage.Components;
//ADT-Tweak-Start
//using Content.Shared.Buckle.Components;
using Content.Shared.Mind.Components;
//ADT-Tweak-End
||||||| base
=======
using Content.Shared.Chat;
>>>>>>> theirs
using Content.Shared.Movement.Events;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Weapons.Misc;
<<<<<<< ours
using Robust.Shared.Network;
//ADT-Tweak-Start
//using Robust.Shared.Audio;
//using Robust.Shared.Audio.Systems;
//ADT-Tweak-End
//using Robust.Shared.Physics.Components; ADT-Tweak

||||||| base
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
=======
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
>>>>>>> theirs
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Chasm;

/// <summary>
/// Handles making entities fall into chasms when stepped on.
/// </summary>
public sealed partial class ChasmSystem : EntitySystem
{
<<<<<<< ours
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedGrapplingGunSystem _grapple = default!;
    //ADT-Tweak-Start
    //[Dependency] private readonly SharedAudioSystem _audio = default!;
    //ADT-Tweak-End
||||||| base
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGrapplingGunSystem _grapple = default!;
=======
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedGrapplingGunSystem _grapple = default!;
>>>>>>> theirs

    [Dependency] private EntityQuery<ChasmComponent> _chasmQuery;
    [Dependency] private EntityQuery<ChasmFallingComponent> _chasmFallingQuery;

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChasmFallingComponent>();
        while (query.MoveNext(out var uid, out var chasm))
        {
            if (_timing.CurTime < chasm.NextDeletionTime)
                continue;

<<<<<<< ours
            // ADT Jaunter start
            RemComp<ChasmFallingComponent>(uid);
            _blocker.UpdateCanMove(uid);

            var ev = new BeforeChasmFallingEvent(uid);
            RaiseLocalEvent(uid, ref ev);
            if (ev.Cancelled)
                continue;
            // ADT Jaunter end
            QueueDel(uid);
||||||| base
            QueueDel(uid);
=======
            var chasmEvent = new EntityCompletedFallingIntoChasmEvent((uid, chasm));
            RaiseLocalEvent(chasm.FallingInto, ref chasmEvent);
            if (_chasmQuery.TryComp(chasm.FallingInto, out var chasmComp))
            {
                var tripperEvent = new CompletedFallingIntoChasmEvent((chasm.FallingInto, chasmComp));
                RaiseLocalEvent(uid, ref tripperEvent);
            }
            else
            {
                DebugTools.Assert($"{ToPrettyString(chasm.FallingInto)} is missing {nameof(ChasmComponent)}");
            }

            PredictedQueueDel(uid);
>>>>>>> theirs
        }
    }

    #region Event Handlers
    [SubscribeLocalEvent]
    private void OnStepTriggered(Entity<ChasmComponent> entity, ref StepTriggeredOffEvent args)
    {
        // already doomed
        if (_chasmFallingQuery.HasComp(args.Tripper))
            return;

<<<<<<< ours
        StartFalling(uid, component, args.Tripper);
    }

    public void StartFalling(EntityUid chasm, ChasmComponent component, EntityUid tripper) //ADT-Tweak
    {
        var falling = AddComp<ChasmFallingComponent>(tripper);
||||||| base
        StartFalling(uid, component, args.Tripper);
    }

    public void StartFalling(EntityUid chasm, ChasmComponent component, EntityUid tripper, bool playSound = true)
    {
        var falling = AddComp<ChasmFallingComponent>(tripper);
=======
        // Check the white-/blacklists and inform on rejection.
        if (!(entity.Comp.Whitelist == null && entity.Comp.Blacklist == null ||
              _whitelist.CheckBoth(args.Tripper, entity.Comp.Blacklist, entity.Comp.Whitelist)))
        {
            var rejected = new FallerRejectedByChasmEvent(args.Tripper);
            RaiseLocalEvent(entity, ref rejected);
            return;
        }
>>>>>>> theirs

        // Give an opportunity to cancel the fall for whatever reason.
        var checkEvent = new EntityStartFallingAttemptEvent(args.Tripper);
        RaiseLocalEvent(entity, ref checkEvent);
        if (checkEvent.Cancelled)
            return;

<<<<<<< ours
        //ADT-Tweak-Start
        //if (playSound)
        //    _audio.PlayPredicted(component.FallingSound, chasm, tripper);
        //ADT-Tweak-End
||||||| base
        if (playSound)
            _audio.PlayPredicted(component.FallingSound, chasm, tripper);
=======
        StartFalling(entity.AsNullable(), args.Tripper);
>>>>>>> theirs
    }

    [SubscribeLocalEvent]
    private void OnStepTriggerAttempt(Entity<ChasmComponent> entity, ref StepTriggerAttemptEvent args)
    {
        if (_grapple.IsEntityHooked(args.Tripper))
        {
            args.Cancelled = true;
            return;
        }

        // ADT Jaunter start
        if (TryComp(args.Tripper, out ADTChasmImmunityComponent? immunity) && _timing.CurTime < immunity.Until)
        {
            args.Cancelled = true;
            return;
        }
        // ADT Jaunter end

        args.Continue = true;
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ChasmComponent> entity, ref ComponentShutdown args)
    {
        var e = EntityQueryEnumerator<ChasmFallingComponent>();
        while (e.MoveNext(out var fallingEnt, out var falling))
        {
            if (falling.FallingInto != entity.Owner)
                continue;

            RemCompDeferred<ChasmFallingComponent>(fallingEnt);
        }
    }

    [SubscribeLocalEvent]
    private static void OnUpdateCanMove(Entity<ChasmFallingComponent> entity, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }
    #endregion Event Handlers

    #region Public API
    /// <summary>
    /// Causes <paramref name="tripper"/> to fall into <paramref name="chasm"/>: starts a falling animation, optionally
    /// plays a sound, and eventually deletes <paramref name="tripper"/>.
    /// If <paramref name="chasm"/> does not have a <see cref="ChasmComponent"/> component, does nothing and returns null.
    /// </summary>
    /// <param name="playSound">Whether or not the chasm should play a sound when the entity falls in.</param>
    /// <param name="playEmote">Whether or not <paramref name="tripper"/> should try to emote when falling into the chasm.</param>
    /// <returns>
    /// <paramref name="tripper"/> with its new <see cref="ChasmFallingComponent"/>, if the entity did start falling, null otherwise.
    /// </returns>
    [PublicAPI]
    public Entity<ChasmFallingComponent>? StartFalling(
        Entity<ChasmComponent?> chasm,
        EntityUid tripper,
        bool playSound = true,
        bool playEmote = true
    )
    {
        if (!_chasmQuery.Resolve(chasm, ref chasm.Comp, logMissing: false))
            return null;

        var falling = AddComp<ChasmFallingComponent>(tripper);
        falling.FallingInto = chasm;

        falling.NextDeletionTime = _timing.CurTime + falling.DeletionTime;
        _blocker.UpdateCanMove(tripper);

        if (playSound)
            _audio.PlayPredicted(chasm.Comp.FallingSound, chasm, tripper);

        if (playEmote && chasm.Comp.Emote is { } emote)
            _chat.TryEmoteWithChat(tripper, emote);

        var chasmEvent = new EntityStartedFallingIntoChasmEvent((tripper, falling));
        RaiseLocalEvent(chasm, ref chasmEvent);
        var tripperEvent = new StartedFallingIntoChasmEvent((chasm, chasm.Comp));
        RaiseLocalEvent(tripper, ref tripperEvent);

        Entity<ChasmFallingComponent> ret = (tripper, falling);
        Dirty(ret);
        return ret;
    }

    #endregion Public API
}
