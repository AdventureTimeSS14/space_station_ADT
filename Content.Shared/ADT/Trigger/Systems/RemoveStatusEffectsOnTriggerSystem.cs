using Content.Shared.StatusEffectNew;
using Content.Shared.Trigger;

namespace Content.Shared.ADT.Trigger;

/// <summary>
/// Обрабатывает <see cref="RemoveStatusEffectsOnTriggerComponent"/>: снимает статус-эффекты с цели.
/// </summary>
public sealed class RemoveStatusEffectsOnTriggerSystem : XOnTriggerSystem<RemoveStatusEffectsOnTriggerComponent>
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    protected override void OnTrigger(Entity<RemoveStatusEffectsOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        foreach (var effect in ent.Comp.Effects)
        {
            if (_status.TryRemoveStatusEffect(target, effect))
                args.Handled = true;
        }
    }
}
