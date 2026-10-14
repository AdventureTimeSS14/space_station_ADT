using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.ADT.Clothing;

/// <summary>
/// Связывает состояние <c>ItemToggle</c> с носителем: окраска живёт на игроке,
/// а включённость — на самой одежде, поэтому нужны оба события.
/// </summary>
public sealed class ADTVisionTintClothingSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTVisionTintClothingComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ADTVisionTintClothingComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<ADTVisionTintClothingComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnEquipped(Entity<ADTVisionTintClothingComponent> ent, ref GotEquippedEvent args)
    {
        if (IsActivated(ent))
            Apply(ent, args.EquipTarget);
    }

    private void OnUnequipped(Entity<ADTVisionTintClothingComponent> ent, ref GotUnequippedEvent args)
    {
        RemComp<ADTVisionTintComponent>(args.EquipTarget);
    }

    private void OnToggled(Entity<ADTVisionTintClothingComponent> ent, ref ItemToggledEvent args)
    {
        if (!_inventory.TryGetContainingSlot(ent.Owner, out _))
            return;

        var wearer = Transform(ent).ParentUid;

        if (args.Activated)
            Apply(ent, wearer);
        else
            RemComp<ADTVisionTintComponent>(wearer);
    }

    private bool IsActivated(EntityUid uid)
    {
        return !TryComp<ItemToggleComponent>(uid, out var toggle) || toggle.Activated;
    }

    private void Apply(Entity<ADTVisionTintClothingComponent> ent, EntityUid wearer)
    {
        var tint = EnsureComp<ADTVisionTintComponent>(wearer);
        tint.Color = ent.Comp.Color;
        tint.Amount = ent.Comp.Amount;
        Dirty(wearer, tint);
    }
}
