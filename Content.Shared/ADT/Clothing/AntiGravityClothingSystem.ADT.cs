using Content.Shared.Clothing.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Clothing.EntitySystems;

public sealed partial class AntiGravityClothingSystem
{
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    [SubscribeLocalEvent]
    private void OnToggled(Entity<AntiGravityClothingComponent> ent, ref ItemToggledEvent args)
    {
        if (_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            _gravity.RefreshWeightless(container.Owner);
    }
}
