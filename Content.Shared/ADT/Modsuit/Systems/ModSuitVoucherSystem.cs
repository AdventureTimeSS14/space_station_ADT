using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.ModSuits;

public sealed class SharedModSuitVoucherSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ModSuitVoucherComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnInteractHand(Entity<ModSuitVoucherComponent> ent, ref InteractHandEvent args)
    {
        if (!args.CanReach)
            return;

        switch (ent.Comp.Current)
        {
            case ModSuitType.MOD:
                ent.Comp.Current = ModSuitType.Hard;
                ent.Comp.State = "hard";
                break;
            default:
                ent.Comp.Current = ModSuitType.MOD;
                ent.Comp.State = "mod";
                break;
        }

        Dirty(ent);
        _popup.PopupEntity($"Voucher switched to {ent.Comp.Current}!", ent, args.User);
    }
}