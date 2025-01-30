using System.Diagnostics;
using System.Linq;
using Content.Shared.ADT.Grab;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Movement.Pulling.Systems;

public sealed partial class PullingSystem
{
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    private void InitializeGrab()
    {
        SubscribeLocalEvent<PullerComponent, GrabStageChangedEvent>(HandleGrabStageChanged);
    }

    private void HandleGrabStageChanged(EntityUid uid, PullerComponent puller, GrabStageChangedEvent args)
    {
        var oldHands = puller.RequiredHands[args.OldStage];
        var newHands = puller.RequiredHands[args.NewStage];
        if (oldHands == newHands)
            return;
        if (!_net.IsServer)
            return;
        if (newHands > oldHands)
        {
            if (!TryComp<HandsComponent>(uid, out var hands))
                return;

            var toSpawn = newHands - oldHands;
            for (var i = 0; i < toSpawn; i++)
            {
                if (_virtualItem.TrySpawnVirtualItemInHand(args.Pulling, uid, out var virtualItem, true))
                    puller.VirtualItems.Add((virtualItem.Value, Comp<VirtualItemComponent>(virtualItem.Value)));
            }
        }
        else
        {
            var toRemove = oldHands - newHands;
            for (var i = 0; i < toRemove; i++)
            {
                _virtualItem.DeleteVirtualItem(puller.VirtualItems.Last(), uid);
            }
        }
    }

    public bool TryStartPullingOrGrab(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        if (puller.Comp.Pulling == pullable.Owner)
            return TryIncreaseGrabStageOrStopPulling(puller, pullable);

        return TryStartPull(puller, pullable);
    }

    public bool TryIncreaseGrabStageOrStopPulling(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        if (puller.Comp.Pulling != pullable.Owner)
            return TryStartPull(puller, pullable);
        if (!_combat.IsInCombatMode(puller.Owner))
            return TryStopPull(pullable, pullable.Comp, puller);

        return TryIncreaseGrabStage(puller, pullable);
    }

    public bool TryLowerGrabStageOrStopPulling(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        if (puller.Comp.Pulling != pullable.Owner)
            return false;
        if (!_combat.IsInCombatMode(puller.Owner))
            return TryStopPull(pullable, pullable.Comp, puller);

        return TryLowerGrabStage(puller, pullable);
    }

    public bool TryIncreaseGrabStage(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        if (puller.Comp.Pulling != pullable.Owner)
            return false;

        if (puller.Comp.Stage == GrabStage.Choke)
            return false;

        // Check if the puller has enough hands to progress to the next stage
        if (puller.Comp.RequiredHands.TryGetValue(puller.Comp.Stage + 1, out var requiredHands))
        {
            var freeableHands = 0;
            if (TryComp<HandsComponent>(puller, out var hands))
                freeableHands = _handsSystem.CountFreeableHands((puller.Owner, hands));

            if (freeableHands < requiredHands)
                return false;
        }

        puller.Comp.Stage++;

        // Switch the popup type based on the new grab stage
        var popupType = PopupType.Small;
        switch (puller.Comp.Stage)
        {
            case GrabStage.Soft:
                popupType = PopupType.Small;
                break;
            case GrabStage.Hard:
                popupType = PopupType.SmallCaution;
                break;
            case GrabStage.Choke:
                popupType = PopupType.MediumCaution;
                break;
            default:
                popupType = PopupType.Small;
                break;
        }

        // Do grab stage change effects
        _popup.PopupPredicted(Loc.GetString($"pulling-system-grab-{puller.Comp.Stage.ToString().ToLower()}-popup"), pullable, puller, popupType);
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), pullable, puller);
        _color.RaiseEffect(Color.Yellow, new() { pullable.Owner }, Filter.Pvs(pullable.Owner));

        // Raise the grab stage changed event
        var message = new GrabStageChangedEvent(puller, pullable, puller.Comp.Stage - 1, puller.Comp.Stage);
        RaiseLocalEvent(puller, ref message);
        RaiseLocalEvent(pullable, ref message);
        Dirty(puller);

        return true;
    }

    public bool TryLowerGrabStage(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        if (puller.Comp.Pulling != pullable.Owner)
            return false;

        // Stop pulling if the puller is at the lowest grab stage
        if (puller.Comp.Stage == GrabStage.None)
        {
            StopPulling(pullable.Owner, pullable);
            return true;
        }

        puller.Comp.Stage--;
        Dirty(puller);

        // Raise the grab stage changed event
        var message = new GrabStageChangedEvent(puller, pullable, puller.Comp.Stage + 1, puller.Comp.Stage);
        RaiseLocalEvent(puller, ref message);
        RaiseLocalEvent(pullable, ref message);

        return true;
    }

    public bool TryEscapeFromGrab(Entity<PullableComponent> pullable, Entity<PullerComponent?> puller)
    {
        if (!Resolve(puller, ref puller.Comp))
            return false;
        if (puller.Comp.Pulling != pullable.Owner)
            return false;
        if (pullable.Comp.EscapeAttemptDoAfter.HasValue)
            return false;

        if (puller.Comp.Stage == GrabStage.None)
        {
            TryStopPull(pullable, pullable.Comp, pullable);
            return true;
        }


        if (puller.Comp.Stage == GrabStage.Choke)
        {
            // If the puller is choking the pullable, they can't escape
            return false;
        }

        if (pullable.Comp.LastEscapeAttempt + TimeSpan.FromSeconds(puller.Comp.EscapeAttemptDelay) > _timing.CurTime)
            return false;

        pullable.Comp.LastEscapeAttempt = _timing.CurTime;

        // Do escape effects
        _popup.PopupPredicted(Loc.GetString("pulling-system-grab-escape-popup"), pullable, pullable, PopupType.Small);
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), pullable, puller);

        var doAfterArgs = new DoAfterArgs(EntityManager, pullable, pullable.Comp.GrabEscapeAttemptTimes[puller.Comp.Stage], new GrabEscapeDoAfterEvent(), null)
        {
            BreakOnDamage = true,
            BreakOnMove = false,
            BreakOnHandChange = false,
            BlockDuplicate = true
        };
        return _doAfter.TryStartDoAfter(doAfterArgs, out pullable.Comp.EscapeAttemptDoAfter);
    }
}
