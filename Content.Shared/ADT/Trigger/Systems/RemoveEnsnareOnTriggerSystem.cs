using System.Linq;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Trigger;

namespace Content.Shared.ADT.Trigger;

public sealed class RemoveEnsnareOnTriggerSystem : XOnTriggerSystem<RemoveEnsnareOnTriggerComponent>
{
    [Dependency] private readonly SharedEnsnareableSystem _ensnareable = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;

    protected override void OnTrigger(Entity<RemoveEnsnareOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (!TryComp<EnsnareableComponent>(target, out var ensnareable) || !ensnareable.IsEnsnared)
            return;

        var freed = false;
        foreach (var ensnare in ensnareable.Container.ContainedEntities.ToArray())
        {
            if (!TryComp<EnsnaringComponent>(ensnare, out var ensnaring))
                continue;

            _ensnareable.ForceFree(ensnare, ensnaring);
            freed = true;
        }

        if (freed)
            _speedModifier.RefreshMovementSpeedModifiers(target);

        args.Handled = true;
    }
}
