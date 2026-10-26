using Content.Shared.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle; // ADT-Tweak
using Content.Shared.Item.ItemToggle.Components; // ADT-Tweak
using Content.Shared.Standing;
using Robust.Shared.Containers; // ADT-Tweak

namespace Content.Shared.Clothing.EntitySystems;

/// <remarks>
/// We check standing state on all clothing because we don't want you to have anti-gravity unless you're standing.
/// This is for balance reasons as it prevents you from wearing anti-grav clothing to cheese being stun cuffed, as
/// well as other worse things.
/// </remarks>
public sealed partial class AntiGravityClothingSystem : EntitySystem
{
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private ItemToggleSystem _toggle = default!; // ADT-Tweak
    [Dependency] private SharedContainerSystem _container = default!; // ADT-Tweak

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AntiGravityClothingComponent, InventoryRelayedEvent<IsWeightlessEvent>>(OnIsWeightless);
        SubscribeLocalEvent<AntiGravityClothingComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<AntiGravityClothingComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<AntiGravityClothingComponent, InventoryRelayedEvent<DownedEvent>>(OnDowned);
        SubscribeLocalEvent<AntiGravityClothingComponent, InventoryRelayedEvent<StoodEvent>>(OnStood);
        SubscribeLocalEvent<AntiGravityClothingComponent, ItemToggledEvent>(OnToggled); // ADT-Tweak
    }

    // ADT-Tweak-Start
    private void OnToggled(Entity<AntiGravityClothingComponent> ent, ref ItemToggledEvent args)
    {
        if (_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            _gravity.RefreshWeightless(container.Owner);
    }
    // ADT-Tweak-End

    private void OnIsWeightless(Entity<AntiGravityClothingComponent> ent, ref InventoryRelayedEvent<IsWeightlessEvent> args)
    {
        if (args.Args.Handled || _standing.IsDown(args.Owner))
            return;

        if (!_toggle.IsActivated(ent.Owner)) // ADT-Tweak
            return;

        args.Args.Handled = true;
        args.Args.IsWeightless = true;
    }

    private void OnEquipped(Entity<AntiGravityClothingComponent> entity, ref ClothingGotEquippedEvent args)
    {
        // This clothing item does nothing if we're not standing
        if (_standing.IsDown(args.Wearer))
            return;

        _gravity.RefreshWeightless(args.Wearer, true);
    }

    private void OnUnequipped(Entity<AntiGravityClothingComponent> entity, ref ClothingGotUnequippedEvent args)
    {
        // This clothing item does nothing if we're not standing
        if (_standing.IsDown(args.Wearer))
            return;

        _gravity.RefreshWeightless(args.Wearer, false);
    }

    private void OnDowned(Entity<AntiGravityClothingComponent> entity, ref InventoryRelayedEvent<DownedEvent> args)
    {
        _gravity.RefreshWeightless(args.Owner, false);
    }

    private void OnStood(Entity<AntiGravityClothingComponent> entity, ref InventoryRelayedEvent<StoodEvent> args)
    {
        _gravity.RefreshWeightless(args.Owner, true);
    }
}
