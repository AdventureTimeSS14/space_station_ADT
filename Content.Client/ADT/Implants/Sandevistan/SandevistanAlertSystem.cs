using Content.Shared.ADT.Implants.Sandevistan;
using Content.Shared.Alert.Components;

namespace Content.Client.ADT.Implants.Sandevistan;

public sealed class SandevistanAlertSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SandevistanLoadComponent, GetGenericAlertCounterAmountEvent>(OnGetCounterAmount);
    }

    private void OnGetCounterAmount(Entity<SandevistanLoadComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled || ent.Comp.LoadAlert != args.Alert)
            return;

        args.Amount = (int) ent.Comp.CurrentLoad;
    }
}
