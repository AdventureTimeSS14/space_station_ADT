using Content.Shared.Actions;
using Content.Shared.ADT.Slimecats;
using Content.Shared.Bed.Sleep;

namespace Content.Server.ADT.Actions.Slimecats;

public sealed partial class SlimecatsSleepActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SleepingSystem _sleepingSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedSlimecatsSleepActionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SharedSlimecatsSleepActionComponent, SharedSlimeCatSleepActionEvent>(OnSleepAction);
        SubscribeLocalEvent<SharedSlimecatsSleepActionComponent, SleepStateChangedEvent>(OnSleepStateChanged);
    }

    private void OnMapInit(EntityUid ent, SharedSlimecatsSleepActionComponent comp, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref comp.ActionEntity, comp.SleepActionForSlimecats);
    }

    private void OnSleepStateChanged(EntityUid uid, SharedSlimecatsSleepActionComponent component, ref SleepStateChangedEvent args)
    {
        if (!args.FellAsleep) // Если кот проснулся
        {
            UpdateAppearance(uid, component, false);
        }
    }

    private void UpdateAppearance(EntityUid uid, SharedSlimecatsSleepActionComponent component, bool isSleeping, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance))
            return;

        component.IsActiveSleep = isSleeping;
        _appearance.SetData(uid, StateSlimcatVisual.Sleep, component.IsActiveSleep, appearance);
    }

    private void OnSleepAction(EntityUid uid, SharedSlimecatsSleepActionComponent component, ref SharedSlimeCatSleepActionEvent args)
    {
        _sleepingSystem.TrySleeping(args.Performer);
        UpdateAppearance(uid, component, true);
    }
}