using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.ADT.ModSuits;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Modsuit;

public sealed class ModSuitPressureSealSystem : EntitySystem
{
    [Dependency] private readonly BarotraumaSystem _barotrauma = default!;
    [Dependency] private readonly ModSuitSystem _modSuits = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModPartComponent, GetPressureProtectionValuesEvent>(OnGetPressureProtection);
        SubscribeLocalEvent<ModPartComponent, GotEquippedEvent>(OnPartEquipped);
        SubscribeLocalEvent<ModPartComponent, EntGotInsertedIntoContainerMessage>(OnPartReturned);

        SubscribeLocalEvent<MagbootsComponent, GotEquippedEvent>(OnMagbootsEquipped);
        SubscribeLocalEvent<MagbootsComponent, GotUnequippedEvent>(OnMagbootsUnequipped);
    }

    private void OnGetPressureProtection(Entity<ModPartComponent> ent, ref GetPressureProtectionValuesEvent args)
    {
        if (IsSuitSealed(ent))
            return;

        args.HighPressureMultiplier = 1f;
        args.HighPressureModifier = 0f;
        args.LowPressureMultiplier = 1f;
        args.LowPressureModifier = 0f;
    }

    private void OnPartEquipped(Entity<ModPartComponent> ent, ref GotEquippedEvent args)
    {
        _barotrauma.RefreshPressureResistance(args.EquipTarget);
    }

    private void OnMagbootsEquipped(Entity<MagbootsComponent> ent, ref GotEquippedEvent args)
    {
        OnMagbootsChanged(args.EquipTarget);
    }

    private void OnMagbootsUnequipped(Entity<MagbootsComponent> ent, ref GotUnequippedEvent args)
    {
        OnMagbootsChanged(args.EquipTarget);
    }

    private void OnMagbootsChanged(EntityUid wearer)
    {
        _barotrauma.RefreshPressureResistance(wearer);
        _modSuits.RefreshWearerSuit(wearer);
    }

    private void OnPartReturned(Entity<ModPartComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ModSuitComponent.DefaultClothingContainerId)
            return;

        if (TryComp<ModSuitComponent>(args.Container.Owner, out var modSuit)
            && modSuit.TempUser is { } wearer)
        {
            _barotrauma.RefreshPressureResistance(wearer);
        }
    }

    private bool IsSuitSealed(Entity<ModPartComponent> ent)
    {
        var suit = GetEntity(ent.Comp.AttachedUid);

        if (!TryComp<ModSuitComponent>(suit, out var modSuit) || modSuit.TempUser is null)
            return true;

        return _modSuits.IsFullyCovered(suit, modSuit);
    }
}
