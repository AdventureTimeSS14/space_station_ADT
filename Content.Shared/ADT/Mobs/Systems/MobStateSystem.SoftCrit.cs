using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Mobs.Systems;

public partial class MobStateSystem
{
    public bool IsSoftCritical(EntityUid target, MobStateComponent? component = null)
    {
        if (!_mobStateQuery.Resolve(target, ref component, false))
            return false;

        return component.CurrentState == MobState.SoftCritical;
    }

    private void OnUpdateCanMove(EntityUid uid, MobStateComponent component, UpdateCanMoveEvent args)
    {
        if (component.CurrentState == MobState.SoftCritical)
            return;

        CheckAct(uid, component, args);
    }
}
