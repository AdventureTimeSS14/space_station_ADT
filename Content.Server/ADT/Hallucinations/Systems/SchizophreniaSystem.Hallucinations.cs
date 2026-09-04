using Content.Server.ADT.Pointing;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.Actions;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.StepTrigger.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Content.Shared.ADT.Chat;
using Content.Shared.ADT.Hallucinations.Components;

namespace Content.Server.ADT.Hallucinations.Systems;

public sealed partial class SchizophreniaSystem
{
    private void InitializeHallucinations()
    {
        SubscribeLocalEvent<HallucinationComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<HallucinationComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<HallucinationComponent, ActionAddedDirectEvent>(OnActionAdded);

        SubscribeLocalEvent<HallucinationComponent, MapInitEvent>(OnHallucinationInit);
        SubscribeLocalEvent<HallucinationComponent, ComponentShutdown>(OnHallucinationShutdown);

        SubscribeLocalEvent<HallucinationComponent, ExpandICChatRecipientsEvent>(OnBeforeChatMessage);
        SubscribeLocalEvent<HallucinationComponent, OverrideEmoteSoundEvent>(OverrideEmoteSound);
        SubscribeLocalEvent<HallucinationComponent, SetupPointingArrowEvent>(OnSetupPointer);

        SubscribeLocalEvent<HallucinationComponent, AttemptMobCollideEvent>(OnMobCollision);
        SubscribeLocalEvent<HallucinationComponent, AttemptMobTargetCollideEvent>(OnMobCollisionTarget);
        SubscribeLocalEvent<HallucinationComponent, PreventCollideEvent>(OnPreventCollision);
        SubscribeLocalEvent<HallucinationComponent, InteractionAttemptEvent>(OnInteractionAttempt);
    }

    #region Pvs overrides
    private void OnPlayerAttached(Entity<HallucinationComponent> ent, ref PlayerAttachedEvent args)
    {
        if (!TryComp<SchizophreniaComponent>(ent.Comp.Ent, out var schizophrenia))
            return;

        foreach (var item in schizophrenia.Hallucinations)
            _pvsOverride.AddForceSend(item, args.Player);
    }

    private void OnPlayerDetached(Entity<HallucinationComponent> ent, ref PlayerDetachedEvent args)
    {
        if (!TryComp<SchizophreniaComponent>(ent.Comp.Ent, out var schizophrenia))
            return;

        foreach (var item in schizophrenia.Hallucinations)
            _pvsOverride.RemoveForceSend(item, args.Player);
    }
    private void OnActionAdded(Entity<HallucinationComponent> ent, ref ActionAddedDirectEvent args)
    {
        AddAsHallucination(ent.Comp.Ent, args.Action);

        if (_player.TryGetSessionByEntity(ent.Owner, out var ourSession))
            _pvsOverride.AddForceSend(args.Action, ourSession);
    }
    private void OnHallucinationInit(Entity<HallucinationComponent> ent, ref MapInitEvent args)
    {
        foreach (var action in _actions.GetActions(ent.Owner))
        {
            AddAsHallucination(ent.Comp.Ent, action);

            if (_player.TryGetSessionByEntity(ent.Owner, out var ourSession))
                _pvsOverride.AddForceSend(action, ourSession);
        }
    }

    private void OnHallucinationShutdown(Entity<HallucinationComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SchizophreniaComponent>(ent.Comp.Ent, out var schizophrenia))
            return;

        schizophrenia.Hallucinations.Remove(ent.Owner);

        if (_player.TryGetSessionByEntity(ent.Comp.Ent, out var session))
            _pvsOverride.RemoveForceSend(ent.Owner, session);

        // For sounds that are deleted really fast but need to be heard by hallucinations
        foreach (var item in schizophrenia.Hallucinations)
        {
            if (_player.TryGetSessionByEntity(item, out var hallucinationSession))
                _pvsOverride.RemoveForceSend(ent.Owner, hallucinationSession);
        }

        if (schizophrenia.Hallucinations.Count <= 0)
            RemComp(ent.Comp.Ent, schizophrenia);
    }
    #endregion

    private void OnBeforeChatMessage(Entity<HallucinationComponent> ent, ref ExpandICChatRecipientsEvent args)
    {
        List<ICommonSession> toRemove = new();

        foreach (var recipient in args.Recipients)
        {
            if (_schizQuery.TryGetComponent(recipient.Key.AttachedEntity, out var schiz) && schiz.Idx == ent.Comp.Idx)
                continue;

            if (_hallucinationQuery.TryGetComponent(recipient.Key.AttachedEntity, out var hallucination) && hallucination.Idx == ent.Comp.Idx)
                continue;

            toRemove.Add(recipient.Key);
        }

        foreach (var item in toRemove)
            args.Recipients.Remove(item);
    }

    private void OverrideEmoteSound(Entity<HallucinationComponent> ent, ref OverrideEmoteSoundEvent args)
    {
        var filter = Filter.Entities(ent.Owner, ent.Comp.Ent);
        var sound = _audio.PlayEntity(args.Sound, filter, ent.Owner, false);

        if (!sound.HasValue)
            return;

        foreach (var recipient in filter.Recipients)
        {
            _pvsOverride.AddForceSend(sound.Value.Entity, recipient);

            AddAsHallucination(ent.Comp.Ent, sound.Value.Entity, false);   // to avoid error spam
        }
    }

    private void OnSetupPointer(Entity<HallucinationComponent> ent, ref SetupPointingArrowEvent args)
    {
        if (_player.TryGetSessionByEntity(ent.Owner, out var hallucinationSession))
            _pvsOverride.AddForceSend(args.Arrow, hallucinationSession);

        AddAsHallucination(ent.Comp.Ent, args.Arrow, false);
    }

    #region Everything interaction-related
    private void OnMobCollision(Entity<HallucinationComponent> ent, ref AttemptMobCollideEvent args)
        => args.Cancelled = true;

    private void OnMobCollisionTarget(Entity<HallucinationComponent> ent, ref AttemptMobTargetCollideEvent args)
        => args.Cancelled = true;

    private void OnPreventCollision(Entity<HallucinationComponent> ent, ref PreventCollideEvent args)
    {
        if (HasComp<StepTriggerComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnInteractionAttempt(Entity<HallucinationComponent> ent, ref InteractionAttemptEvent args)
        => args.Cancelled = true;
    #endregion
}
