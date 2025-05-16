using Content.Shared.ADT.Cytology.Components.Container;
using Content.Shared.ADT.Cytology.Events;
using Content.Shared.ADT.Cytology.Visuals;

namespace Content.Shared.ADT.Cytology.Systems;

public abstract class SharedCellVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellContainerVisualsComponent, CellAdded>(OnCellAdded);
        SubscribeLocalEvent<CellContainerVisualsComponent, CellRemoved>(OnCellRemoved);
    }

    private void OnCellAdded(Entity<CellContainerVisualsComponent> ent, ref CellAdded args)
    {
        UpdateAppearance(ent);
    }

    private void OnCellRemoved(Entity<CellContainerVisualsComponent> ent, ref CellRemoved args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<CellContainerVisualsComponent> ent)
    {
        if (!TryComp<CellContainerComponent>(ent, out var containerComponent))
            return;

        _appearance.SetData(ent, CellContainerVisuals.DishVisibility, !containerComponent.Empty);
    }
}
