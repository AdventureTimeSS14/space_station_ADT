using Content.Server.Popups;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.ADT.Xenobiology.Potions;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Interaction;

namespace Content.Server.ADT.Xenobiology.Potions;

/// <summary>
/// Applies the slime fireproof potion: full heat and fire damage protection.
/// </summary>
public sealed partial class SlimeFireproofPotionSystem : EntitySystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly FireProtectionSystem _fireProtection = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeFireproofPotionComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<SlimeFireproofPotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        args.Handled = true;
        var changed = false;

        var temperatureProtection = EnsureComp<TemperatureProtectionComponent>(target);
        if (temperatureProtection.HeatingCoefficient > 0f || temperatureProtection.CoolingCoefficient > 0f)
        {
            _temperature.SetHeatProtection((target, temperatureProtection), 0f);
            changed = true;
        }

        var fireProtection = EnsureComp<FireProtectionComponent>(target);
        if (fireProtection.Reduction < 1f)
        {
            _fireProtection.SetFireProtection(fireProtection, 1f);
            changed = true;
        }

        if (!changed)
        {
            _popup.PopupEntity(Loc.GetString("xeno-potion-fireproof-max"), args.User, args.User);
            return;
        }

        ent.Comp.RemainingUses -= 1;
        _popup.PopupEntity(Loc.GetString("xeno-potion-fireproof-applied", ("uses", ent.Comp.RemainingUses)), args.User, args.User);

        if (ent.Comp.RemainingUses <= 0)
            PredictedQueueDel(args.Used);
    }
}
