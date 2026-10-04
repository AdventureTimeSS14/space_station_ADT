using Content.Shared.Ensnaring;
using Content.Shared.Movement.Systems;
using Content.Shared.Trigger;

namespace Content.Shared.ADT.Trigger;

public sealed class RemoveEnsnareOnTriggerSystem : XOnTriggerSystem<RemoveEnsnareOnTriggerComponent>
{
    [Dependency] private SharedEnsnareableSystem _ensnareable = default!;
    [Dependency] private MovementSpeedModifierSystem _speedModifier = default!;

    protected override void OnTrigger(Entity<RemoveEnsnareOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (!_ensnareable.IsEnsnared(target))
            return;

        if (_ensnareable.ForceFreeAll(target).Count > 0)
            _speedModifier.RefreshMovementSpeedModifiers(target);

        args.Handled = true;
    }
}
