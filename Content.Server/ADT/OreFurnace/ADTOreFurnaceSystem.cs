using System.Linq;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.ADT.OreFurnace;
using Content.Shared.ADT.OreFurnace.Components;
using Content.Shared.ADT.OreFurnace.Prototypes;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.ADT.Salvage.Systems;
using Content.Shared.Materials;
using Content.Shared.Stacks;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.OreFurnace;

public sealed class ADTOreFurnaceSystem : EntitySystem
{
    [Dependency] private readonly ADTSharedOreFurnaceSystem _furnace = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MiningPointsSystem _miningPoints = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTOreFurnaceComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTOreFurnaceComponent, GetMaterialWhitelistEvent>(OnGetWhitelist);
        SubscribeLocalEvent<ADTOreFurnaceComponent, MaterialAmountChangedEvent>(OnMaterialAmountChanged);
        SubscribeLocalEvent<ADTOreFurnaceComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);
        SubscribeLocalEvent<ADTOreFurnaceComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<ADTOreFurnaceComponent, UpgradeExamineEvent>(OnUpgradeExamine);

        Subs.BuiEvents<ADTOreFurnaceComponent>(ADTOreFurnaceUiKey.Key, subs =>
        {
            subs.Event<ADTOreFurnaceSmeltMessage>(OnSmelt);
            subs.Event<ADTOreFurnaceSmeltAllMessage>(OnSmeltAll);
            subs.Event<ADTOreFurnaceClaimPointsMessage>(OnClaimPoints);
        });
    }

    private void OnMapInit(Entity<ADTOreFurnaceComponent> ent, ref MapInitEvent args)
    {
        _materialStorage.UpdateMaterialWhitelist(ent.Owner);
    }

    private void OnGetWhitelist(Entity<ADTOreFurnaceComponent> ent, ref GetMaterialWhitelistEvent args)
    {
        if (args.Storage != ent.Owner)
            return;

        var whitelist = new List<ProtoId<MaterialPrototype>>();

        foreach (var recipe in _furnace.GetRecipes(ent.Comp))
        {
            foreach (var (material, _) in recipe.Materials)
            {
                if (!whitelist.Contains(material))
                    whitelist.Add(material);
            }
        }

        args.Whitelist = args.Whitelist.Union(whitelist).ToList();
    }

    private void OnMaterialAmountChanged(Entity<ADTOreFurnaceComponent> ent, ref MaterialAmountChangedEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnBeforeUiOpen(Entity<ADTOreFurnaceComponent> ent, BeforeActivatableUIOpenEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnRefreshParts(EntityUid uid, ADTOreFurnaceComponent component, RefreshPartsEvent args)
    {
        var laserTier = args.GetPartRating(MachinePartIds.MicroLaser, 1f);
        var binTier = args.GetPartRating(MachinePartIds.MatterBin, 1f);

        component.OutputMultiplier = RefreshPartsEvent.GetTierMultiplier(laserTier, 0.20f)
            * RefreshPartsEvent.GetTierMultiplier(binTier, -0.10f);

        component.PointsMultiplier = RefreshPartsEvent.GetTierMultiplier(binTier, 0.20f)
            * RefreshPartsEvent.GetTierMultiplier(laserTier, -0.10f);

        Dirty(uid, component);
        UpdateUiState((uid, component));
    }

    private static void OnUpgradeExamine(EntityUid uid, ADTOreFurnaceComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-ore-output", component.OutputMultiplier, benefit: true);
        args.AddPercentageUpgrade("machine-upgrade-ore-points", component.PointsMultiplier, benefit: true);
    }

    private void OnSmelt(Entity<ADTOreFurnaceComponent> ent, ref ADTOreFurnaceSmeltMessage args)
    {
        if (!_furnace.HasRecipe(ent.Comp, args.Recipe))
            return;

        if (!_proto.TryIndex(args.Recipe, out var recipe))
            return;

        if (TrySmelt(ent, recipe, args.Amount) > 0)
            _audio.PlayPvs(ent.Comp.SmeltSound, ent);

        UpdateUiState(ent);
    }

    private void OnSmeltAll(Entity<ADTOreFurnaceComponent> ent, ref ADTOreFurnaceSmeltAllMessage args)
    {
        var smelted = 0;

        foreach (var recipe in _furnace.GetRecipes(ent.Comp))
        {
            smelted += TrySmelt(ent, recipe, ent.Comp.MaxSmeltAmount);
        }

        if (smelted > 0)
            _audio.PlayPvs(ent.Comp.SmeltSound, ent);

        UpdateUiState(ent);
    }

    private void OnClaimPoints(Entity<ADTOreFurnaceComponent> ent, ref ADTOreFurnaceClaimPointsMessage args)
    {
        if (_miningPoints.TryFindIdCard(args.Actor) is { } card)
            _miningPoints.TransferAll(ent.Owner, card);

        UpdateUiState(ent);
    }

    public int TrySmelt(Entity<ADTOreFurnaceComponent> ent, OreSmeltRecipePrototype recipe, int amount)
    {
        amount = Math.Min(amount, _furnace.GetMaxSmeltAmount(ent, recipe));

        if (amount <= 0)
            return 0;

        foreach (var (material, needed) in recipe.Materials)
        {
            var cost = _furnace.GetMaterialCost(ent.Comp, needed) * amount;
            _materialStorage.TryChangeMaterialAmount(ent.Owner, material, -cost);
        }

        SpawnResult(ent.Owner, recipe.Result, _furnace.GetOutputCount(ent.Comp, amount));

        var points = _furnace.GetPointsGain(ent.Comp, recipe, amount);
        if (points > 0)
            _miningPoints.AddPoints(ent.Owner, points);

        return amount;
    }

    private void SpawnResult(EntityUid uid, EntProtoId result, int count)
    {
        if (count <= 0)
            return;

        var coords = Transform(uid).Coordinates;
        var perStack = _stack.GetMaxCount(result);

        while (count > 0)
        {
            var spawned = Spawn(result, coords);
            var inStack = 1;

            if (TryComp<StackComponent>(spawned, out var stack))
            {
                inStack = Math.Min(count, perStack);
                _stack.SetCount((spawned, stack), inStack);
            }

            count -= inStack;
            _stack.TryMergeToContacts(spawned);
        }
    }

    private void UpdateUiState(Entity<ADTOreFurnaceComponent> ent)
    {
        var hasPoints = TryComp<MiningPointsComponent>(ent, out var points);
        _ui.SetUiState(ent.Owner, ADTOreFurnaceUiKey.Key, new ADTOreFurnaceUpdateState(points?.Points ?? 0, hasPoints));
    }
}
