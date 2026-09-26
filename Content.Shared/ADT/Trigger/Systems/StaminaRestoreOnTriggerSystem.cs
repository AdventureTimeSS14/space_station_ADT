using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Trigger;

namespace Content.Shared.ADT.Trigger;

/// <summary>
/// Обрабатывает <see cref="StaminaRestoreOnTriggerComponent"/>: выводит цель из стамкрита и полностью восстанавливает стамину.
/// </summary>
public sealed class StaminaRestoreOnTriggerSystem : XOnTriggerSystem<StaminaRestoreOnTriggerComponent>
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    protected override void OnTrigger(Entity<StaminaRestoreOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (!TryComp<StaminaComponent>(target, out var stamina))
            return;

        // В стамкрите урон по стамине не меняется, поэтому сначала выводим из него.
        _stamina.ExitStamCrit(target, stamina);

        // Заведомо больше любого урона (значение не может уйти ниже нуля), сопротивление стамине игнорируем.
        _stamina.TakeStaminaDamage(target, -stamina.CritThreshold * 100f, stamina, source: args.User, with: ent.Owner, visual: false, ignoreResist: true);

        args.Handled = true;
    }
}
