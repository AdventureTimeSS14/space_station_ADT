using Content.Shared.ADT.Silicons.Borgs;
using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed class BorgSwitchableSubtypeSystem : SharedBorgSwitchableSubtypeSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, BorgSelectSubtypeMessage>(OnSubtypeSelected);
    }

    private void OnSubtypeSelected(Entity<BorgSwitchableSubtypeComponent> ent, ref BorgSelectSubtypeMessage args)
    {
        if (TryComp<BorgSwitchableTypeComponent>(ent.Owner, out var typeComp)
            && typeComp.SelectedBorgType is { } selected
            && Prototypes.TryIndex(args.Subtype, out var subtype)
            && subtype.ParentBorgType != selected)
            return;

        ent.Comp.BorgSubtype = args.Subtype;
        Dirty(ent);
        UpdateVisuals(ent);
        _userInterface.CloseUi((ent.Owner, null), BorgSwitchableTypeUiKey.SelectBorgType);
    }
}
