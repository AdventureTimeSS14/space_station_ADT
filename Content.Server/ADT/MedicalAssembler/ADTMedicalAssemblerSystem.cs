using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.ADT.MedicalAssembler;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Content.Shared.Research.Systems;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.ADT.MedicalAssembler;
public sealed class ADTMedicalAssemblerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedResearchSystem _research = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<ADTMedicalAssemblerComponent>(MedicalAssemblerUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<MedicalAssemblerCraftMessage>(OnCraftMessage);
            subs.Event<MedicalAssemblerEjectMaterialsMessage>(OnEjectMaterialsMessage);
            subs.Event<MedicalAssemblerCreateCustomMessage>(OnCreateCustomMessage);
        });

        SubscribeLocalEvent<ADTMedicalAssemblerComponent, EntInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<ADTMedicalAssemblerComponent, EntRemovedFromContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<ADTMedicalAssemblerComponent, RefreshPartsEvent>(OnRefreshParts);
    }

    private void OnRefreshParts(Entity<ADTMedicalAssemblerComponent> ent, ref RefreshPartsEvent args)
    {
        ent.Comp.CustomTabUnlocked = args.GetPartRating(MachinePartIds.MatterBin, 1f) >= 4f;
        UpdateUi(ent);
    }

    private void OnUiOpened(Entity<ADTMedicalAssemblerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnContainerChanged(Entity<ADTMedicalAssemblerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateUi(ent);
    }

    private void OnContainerChanged(Entity<ADTMedicalAssemblerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateUi(ent);
    }

    private void OnCraftMessage(Entity<ADTMedicalAssemblerComponent> ent, ref MedicalAssemblerCraftMessage msg)
    {
        if (!_power.IsPowered(ent)
            || !_proto.TryIndex<FoodRecipePrototype>(msg.RecipeId, out var recipe)
            || (recipe.RecipeType & (int)MicrowaveRecipeType.MedicalAssembler) == 0
            || !IsRecipeUnlocked(ent, recipe)
            || !_container.TryGetContainer(ent.Owner, ADTMedicalAssemblerComponent.MaterialsContainerId, out var storage))
        {
            return;
        }

        foreach (var (solidProtoId, quantity) in recipe.IngredientsSolids)
        {
            if (CountMaterial(storage, solidProtoId) < quantity)
                return;
        }

        Entity<SolutionComponent>? beakerSolEnt = null;
        Solution? beakerSolution = null;
        if (recipe.IngredientsReagents.Count > 0)
        {
            var beaker = _itemSlots.GetItemOrNull(ent.Owner, ADTMedicalAssemblerComponent.BeakerSlotId);
            if (beaker is not { Valid: true }
                || !_solution.TryGetFitsInDispenser(beaker.Value, out beakerSolEnt, out beakerSolution))
            {
                return;
            }

            var reagentMultiplier = GetReagentMultiplier(ent);
            foreach (var (reagentId, quantity) in recipe.IngredientsReagents)
            {
                if (beakerSolution.GetTotalPrototypeQuantity(reagentId) < GetRequiredReagent(quantity, reagentMultiplier))
                    return;
            }
        }

        foreach (var (solidProtoId, quantity) in recipe.IngredientsSolids)
        {
            var remaining = (int) quantity;
            foreach (var item in storage.ContainedEntities.ToArray())
            {
                if (remaining <= 0)
                    break;

                if (GetPrototypeId(item) != solidProtoId)
                    continue;

                QueueDel(item);
                remaining--;
            }
        }

        if (beakerSolEnt is { } solEnt && beakerSolution is { } sol)
        {
            var reagentMultiplier = GetReagentMultiplier(ent);
            foreach (var (reagentId, quantity) in recipe.IngredientsReagents)
                _solution.RemoveReagent(solEnt, reagentId, GetRequiredReagent(quantity, reagentMultiplier));

            _solution.UpdateChemicals(solEnt);
        }

        for (var i = 0; i < recipe.ResultCount; i++)
            Spawn(recipe.Result, Transform(ent.Owner).Coordinates);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg"), ent);

        UpdateUi(ent);
    }

    private void OnEjectMaterialsMessage(Entity<ADTMedicalAssemblerComponent> ent, ref MedicalAssemblerEjectMaterialsMessage msg)
    {
        if (!_container.TryGetContainer(ent.Owner, ADTMedicalAssemblerComponent.MaterialsContainerId, out var storage))
            return;

        _container.EmptyContainer(storage);
        UpdateUi(ent);
    }

    private void OnCreateCustomMessage(Entity<ADTMedicalAssemblerComponent> ent, ref MedicalAssemblerCreateCustomMessage msg)
    {
        if (!_power.IsPowered(ent) || !ent.Comp.CustomTabUnlocked || msg.ItemType != "medipen")
            return;

        var resultProto = ent.Comp.MedipenStylePrototypes.GetValueOrDefault(msg.StyleId);
        if (resultProto is null)
            return;

        var reagents = new Dictionary<string, FixedPoint2>();
        foreach (var reagentQuantity in msg.Reagents)
        {
            if (reagentQuantity.Quantity <= 0)
                continue;

            var reagentId = reagentQuantity.Reagent.Prototype;
            reagents[reagentId] = reagents.GetValueOrDefault(reagentId) + reagentQuantity.Quantity;
        }

        var total = FixedPoint2.Zero;
        foreach (var quantity in reagents.Values)
            total += quantity;

        if (total <= 0 || total > FixedPoint2.New(ent.Comp.MaxCustomVolume))
            return;

        var beaker = _itemSlots.GetItemOrNull(ent.Owner, ADTMedicalAssemblerComponent.BeakerSlotId);
        if (beaker is not { Valid: true }
            || !_solution.TryGetFitsInDispenser(beaker.Value, out var beakerSolEnt, out var beakerSolution))
        {
            return;
        }

        foreach (var (reagentId, quantity) in reagents)
        {
            if (beakerSolution.GetTotalPrototypeQuantity(reagentId) < quantity)
                return;
        }

        foreach (var (reagentId, quantity) in reagents)
            _solution.RemoveReagent(beakerSolEnt.Value, reagentId, quantity);

        _solution.UpdateChemicals(beakerSolEnt.Value);

        var result = Spawn(resultProto, Transform(ent.Owner).Coordinates);
        if (_solution.TryGetSolution(result, "pen", out var resultSolEnt, out var resultSolution))
        {
            foreach (var (reagentId, quantity) in reagents)
                resultSolution.AddReagent(reagentId, quantity);

            _solution.UpdateChemicals(resultSolEnt.Value);
        }

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg"), ent);
        UpdateUi(ent);
    }

    private bool IsRecipeUnlocked(Entity<ADTMedicalAssemblerComponent> ent, FoodRecipePrototype recipe)
    {
        if (recipe.RequiredTechnology is not { } tech)
            return true;

        return _research.IsTechnologyUnlocked(ent.Owner, tech);
    }

    private int CountMaterial(BaseContainer storage, string protoId)
    {
        var count = 0;
        foreach (var item in storage.ContainedEntities)
        {
            if (GetPrototypeId(item) == protoId)
                count++;
        }

        return count;
    }

    private string? GetPrototypeId(EntityUid uid)
    {
        return MetaData(uid).EntityPrototype?.ID;
    }

    private float GetReagentMultiplier(Entity<ADTMedicalAssemblerComponent> ent)
    {
        return TryComp<AssemblerUpgradeComponent>(ent, out var upgrade) ? upgrade.IngredientMultiplier : 1f;
    }

    private static FixedPoint2 GetRequiredReagent(FixedPoint2 quantity, float multiplier)
    {
        return FixedPoint2.New(quantity.Float() * multiplier);
    }

    private void UpdateUi(Entity<ADTMedicalAssemblerComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, MedicalAssemblerUiKey.Key))
            return;

        var recipes = new List<MedicalAssemblerRecipeEntry>();
        foreach (var recipe in _proto.EnumeratePrototypes<FoodRecipePrototype>())
        {
            if ((recipe.RecipeType & (int)MicrowaveRecipeType.MedicalAssembler) == 0)
                continue;

            recipes.Add(new MedicalAssemblerRecipeEntry(recipe.ID, IsRecipeUnlocked(ent, recipe)));
        }

        var materials = new List<MedicalAssemblerMaterialEntry>();
        if (_container.TryGetContainer(ent.Owner, ADTMedicalAssemblerComponent.MaterialsContainerId, out var storage))
        {
            var counts = new Dictionary<string, int>();
            foreach (var item in storage.ContainedEntities)
            {
                var protoId = GetPrototypeId(item);
                if (protoId is null)
                    continue;

                counts[protoId] = counts.GetValueOrDefault(protoId) + 1;
            }

            materials.AddRange(counts.Select(entry => new MedicalAssemblerMaterialEntry(entry.Key, entry.Value)));
        }

        var beaker = _itemSlots.GetItemOrNull(ent.Owner, ADTMedicalAssemblerComponent.BeakerSlotId);
        List<ReagentQuantity>? beakerReagents = null;
        var beakerVolume = 0f;
        var beakerCapacity = 0f;
        if (beaker is { Valid: true }
            && _solution.TryGetFitsInDispenser(beaker.Value, out var beakerSolEnt, out var beakerSolution))
        {
            beakerReagents = beakerSolution.Contents;
            beakerVolume = beakerSolution.Volume.Float();
            beakerCapacity = beakerSolution.MaxVolume.Float();
        }

        _ui.SetUiState(ent.Owner, MedicalAssemblerUiKey.Key, new MedicalAssemblerUpdateState(
            recipes.ToArray(),
            materials.ToArray(),
            beakerReagents,
            beakerVolume,
            beakerCapacity,
            beakerReagents != null,
            GetReagentMultiplier(ent),
            ent.Comp.CustomTabUnlocked,
            ent.Comp.MaxCustomVolume));
    }
}
