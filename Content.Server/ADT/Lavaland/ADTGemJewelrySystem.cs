using Content.Shared.ADT.Clothing.Accessories;
using Content.Shared.ADT.Lavaland;
using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTGemJewelrySystem : EntitySystem
{
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private const string NoGem = "none";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTGemJewelryComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTGemJewelryComponent, EntInsertedIntoContainerMessage>(OnGemInserted);
        SubscribeLocalEvent<ADTGemJewelryComponent, EntRemovedFromContainerMessage>(OnGemRemoved);
        SubscribeLocalEvent<ADTGemJewelryComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ADTGemJewelryComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<ADTGemJewelryComponent, ADTAccessoryWornChangedEvent>(OnAccessoryWorn);
    }

    private void OnMapInit(Entity<ADTGemJewelryComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.BaseName = MetaData(ent.Owner).EntityName;
        _appearance.SetData(ent.Owner, ADTJewelryVisuals.Gem, NoGem);
    }

    private void OnGemInserted(Entity<ADTGemJewelryComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.Slot)
            return;

        var key = CompOrNull<ADTGemComponent>(args.Entity)?.JewelryKey;
        SetKey(ent, key);

        if (_light.TryGetLight(args.Entity, out var gemLight) && _light.TryGetLight(ent.Owner, out var light))
        {
            _light.SetColor(ent.Owner, gemLight.Color, light);
            _light.SetEnabled(ent.Owner, true, light);
        }
    }

    private void OnGemRemoved(Entity<ADTGemJewelryComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.Slot)
            return;

        SetKey(ent, null);

        if (_light.TryGetLight(ent.Owner, out var light))
            _light.SetEnabled(ent.Owner, false, light);
    }

    private void OnEquipped(Entity<ADTGemJewelryComponent> ent, ref GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(ent.Owner, out var clothing) || (clothing.Slots & args.SlotFlags) == 0)
            return;

        SetWearer(ent, args.Equipee);
    }

    private void OnUnequipped(Entity<ADTGemJewelryComponent> ent, ref GotUnequippedEvent args)
    {
        if (ent.Comp.Wearer == args.Equipee)
            SetWearer(ent, null);
    }

    private void OnAccessoryWorn(Entity<ADTGemJewelryComponent> ent, ref ADTAccessoryWornChangedEvent args)
    {
        if (args.Worn)
        {
            SetWearer(ent, args.Wearer);
            return;
        }

        if (ent.Comp.Wearer == args.Wearer)
            SetWearer(ent, null);
    }

    private void SetKey(Entity<ADTGemJewelryComponent> ent, string? key)
    {
        var wearer = ent.Comp.Wearer;
        RemoveEffect(ent);

        ent.Comp.Key = key;
        _appearance.SetData(ent.Owner, ADTJewelryVisuals.Gem, key ?? NoGem);
        _clothing.SetEquippedPrefix(ent.Owner, key);

        if (TryComp<ADTAccessoryComponent>(ent.Owner, out var accessory))
        {
            accessory.EquippedState = key == null
                ? $"equipped-{ent.Comp.EquippedSlot}"
                : $"{key}-equipped-{ent.Comp.EquippedSlot}";
            Dirty(ent.Owner, accessory);

            if (_container.TryGetContainingContainer(ent.Owner, out var holder) && HasComp<ADTAccessoryHolderComponent>(holder.Owner))
                _item.VisualsChanged(holder.Owner);
        }

        var name = key == null
            ? ent.Comp.BaseName
            : Loc.GetString($"{ent.Comp.NamePrefix}-{key}");

        if (name != null)
            _meta.SetEntityName(ent.Owner, name);

        ent.Comp.Wearer = wearer;
        ApplyEffect(ent);
    }

    private void SetWearer(Entity<ADTGemJewelryComponent> ent, EntityUid? wearer)
    {
        RemoveEffect(ent);
        ent.Comp.Wearer = wearer;
        ApplyEffect(ent);
    }

    private void ApplyEffect(Entity<ADTGemJewelryComponent> ent)
    {
        if (ent.Comp.Wearer is not { } wearer || ent.Comp.Key is not { } key)
            return;

        if (!ent.Comp.KeyEffects.TryGetValue(key, out var effect))
            return;

        _status.TryAddStatusEffect(wearer, effect, out _);
    }

    private void RemoveEffect(Entity<ADTGemJewelryComponent> ent)
    {
        if (ent.Comp.Wearer is not { } wearer || ent.Comp.Key is not { } key)
            return;

        if (!ent.Comp.KeyEffects.TryGetValue(key, out var effect))
            return;

        _status.TryRemoveStatusEffect(wearer, effect);
    }
}
