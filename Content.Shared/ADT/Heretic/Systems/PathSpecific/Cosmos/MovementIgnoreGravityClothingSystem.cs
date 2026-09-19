//

using Content.Shared.Clothing;
using Content.Shared.Gravity;
using Content.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Shared.Inventory;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Cosmos;

public sealed partial class MovementIgnoreGravityClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<MovementIgnoreGravityClothingComponent, IsWeightlessEvent>(OnIsWeightless,
            held: false,
            baseEvent: false);

        SubscribeLocalEvent<MovementIgnoreGravityClothingComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<MovementIgnoreGravityClothingComponent, ClothingGotUnequippedEvent>(OnUnequip);
    }

    private void OnUnequip(Entity<MovementIgnoreGravityClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        _gravity.RefreshWeightless(args.Wearer, true);
    }

    private void OnEquip(Entity<MovementIgnoreGravityClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        EnsureComp<GravityAffectedComponent>(args.Wearer);
        _gravity.RefreshWeightless(args.Wearer, ent.Comp.Weightless);
    }

    private void OnIsWeightless(Entity<MovementIgnoreGravityClothingComponent> ent, ref IsWeightlessEvent args)
    {
        args.IsWeightless = ent.Comp.Weightless;
        args.Handled = true;
    }
}
