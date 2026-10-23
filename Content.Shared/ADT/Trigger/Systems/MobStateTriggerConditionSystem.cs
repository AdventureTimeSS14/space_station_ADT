using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Trigger;

namespace Content.Shared.ADT.Trigger;

public sealed class MobStateTriggerConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateTriggerConditionComponent, AttemptTriggerEvent>(OnAttempt);
    }

    private void OnAttempt(Entity<MobStateTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.Keys.Contains(args.Key))
            return;

        if (args.User is not { } user)
            return;

        if (TryComp<MobStateComponent>(user, out var mobState) && ent.Comp.AllowedStates.Contains(mobState.CurrentState))
            return;

        args.Cancelled = true;

        if (ent.Comp.FailPopup is { } popup)
            _popup.PopupClient(Loc.GetString(popup), user, user, PopupType.SmallCaution);
    }
}
