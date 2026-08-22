using Content.Shared.ADT.Silicons.Borgs;
using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed class BorgSwitchableSubtypeSystem : SharedBorgSwitchableSubtypeSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, BorgSelectTypeMessage>(OnTypeSelected);
    }

    // ADT-Tweak: подтип выбирается тем же сообщением, что и тип, чтобы модули и скин применялись вместе
    private void OnTypeSelected(Entity<BorgSwitchableSubtypeComponent> ent, ref BorgSelectTypeMessage args)
    {
        if (args.Subtype is not { } subtype)
            return;

        // Тип уже выбран и не совпадает - отклоняем сообщение, чтобы не записать чужой подтип
        if (TryComp<BorgSwitchableTypeComponent>(ent.Owner, out var typeComp)
            && typeComp.SelectedBorgType is { } selected
            && selected != args.Prototype)
            return;

        ent.Comp.BorgSubtype = subtype;
        Dirty(ent);
        UpdateVisuals(ent);
        _userInterface.CloseUi((ent.Owner, null), BorgSwitchableTypeUiKey.SelectBorgType);
    }
}
