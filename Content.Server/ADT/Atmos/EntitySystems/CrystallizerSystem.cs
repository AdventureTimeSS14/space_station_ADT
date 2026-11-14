using Content.Server.ADT.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.ADT.Atmos.Components;
using Content.Shared.ADT.Atmos.Prototypes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Server.ADT.Atmos.Systems;

/// <summary>
/// Машина, кристаллизующая газовую смесь из входной трубы в предметы при соблюдении рецепта.
/// </summary>
public sealed partial class CrystallizerSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _map = default!;

    private const float MinProgressAmount = 3f;
    private const float RecipeMolesLogScale = 0.1f;
    private const float StallQualityPenalty = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrystallizerComponent, BeforeActivatableUIOpenEvent>(OnBeforeOpened);
        SubscribeLocalEvent<CrystallizerComponent, CrystallizerToggleMessage>(OnToggleMessage);
        SubscribeLocalEvent<CrystallizerComponent, CrystallizerSelectRecipeMessage>(OnSelectRecipeMessage);
        SubscribeLocalEvent<CrystallizerComponent, CrystallizerSetGasInputMessage>(OnSetGasInputMessage);
        SubscribeLocalEvent<CrystallizerComponent, AtmosDeviceUpdateEvent>(OnDeviceAtmosUpdate);
    }

    private void OnBeforeOpened(Entity<CrystallizerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        DirtyUI(ent, ent.Comp);
    }

    private void OnToggleMessage(EntityUid uid, CrystallizerComponent crystallizer, CrystallizerToggleMessage args)
    {
        _power.TogglePower(uid);
        DirtyUI(uid, crystallizer);
    }

    private void OnSelectRecipeMessage(EntityUid uid, CrystallizerComponent crystallizer, CrystallizerSelectRecipeMessage args)
    {
        if (args.RecipeId == crystallizer.SelectedRecipeId) return;
        if (GetInputPipeMixture(uid, crystallizer, out var pipeMix) && pipeMix != null)
        {
            _atmos.Merge(pipeMix, crystallizer.CrystallizerGasMixture);
            crystallizer.CrystallizerGasMixture = new();
        }
        crystallizer.SelectedRecipeId = args.RecipeId;
        crystallizer.ProgressBar = 0f;
        crystallizer.QualityLoss = 0f;
        crystallizer.TotalRecipeMoles = CalculateTotalRecipeMoles(crystallizer);
        UpdateProgressBarUI(uid, crystallizer);
        DirtyUI(uid, crystallizer);
    }

    private void OnSetGasInputMessage(EntityUid uid, CrystallizerComponent crystallizer, CrystallizerSetGasInputMessage args)
    {
        crystallizer.GasInput = args.GasInput;
        DirtyUI(uid, crystallizer);
    }

    private void OnDeviceAtmosUpdate(EntityUid uid, CrystallizerComponent crystallizer, ref AtmosDeviceUpdateEvent args)
    {
        if (!_power.IsPowered(uid) || !TryComp<ApcPowerReceiverComponent>(uid, out _))
            return;

        ProcessGasInput(uid, crystallizer);
        ProcessRecipe(uid, crystallizer);
        ProcessTemperatureRegulator(uid, crystallizer, args.dt);
        UpdateGasMixtureUI(uid, crystallizer);
    }

    private void DirtyUI(EntityUid uid, CrystallizerComponent? crystallizer = null, UserInterfaceComponent? ui = null)
    {
        if (!Resolve(uid, ref crystallizer, ref ui, false))
            return;

        ApcPowerReceiverComponent? powerReceiver = null;
        if (!Resolve(uid, ref powerReceiver))
            return;

        _userInterfaceSystem.SetUiState(uid, CrystallizerUiKey.Key,
            new CrystallizerBoundUserInterfaceState(
                !powerReceiver.PowerDisabled,
                crystallizer.SelectedRecipeId,
                crystallizer.GasInput,
                crystallizer.CrystallizerGasMixture,
                crystallizer.ProgressBar));
    }

    private void UpdateGasMixtureUI(EntityUid uid, CrystallizerComponent? crystallizer = null, UserInterfaceComponent? ui = null)
    {
        if (!Resolve(uid, ref crystallizer, ref ui, false))
            return;

        _userInterfaceSystem.ServerSendUiMessage(uid, CrystallizerUiKey.Key,
            new CrystallizerUpdateGasMixtureMessage(crystallizer.CrystallizerGasMixture));
    }

    private void UpdateProgressBarUI(EntityUid uid, CrystallizerComponent? crystallizer = null, UserInterfaceComponent? ui = null)
    {
        if (!Resolve(uid, ref crystallizer, ref ui, false))
            return;

        _userInterfaceSystem.ServerSendUiMessage(uid, CrystallizerUiKey.Key,
            new CrystallizerProgressBarMessage(crystallizer.ProgressBar));
    }

    private void ProcessGasInput(EntityUid uid, CrystallizerComponent crystallizer)
    {
        var crystalMix = crystallizer.CrystallizerGasMixture;
        if (GetInputPipeMixture(uid, crystallizer, out var pipeMix) && pipeMix != null)
        {
            if (string.IsNullOrEmpty(crystallizer.SelectedRecipeId) ||
                !_prototypeManager.TryIndex<CrystallizerRecipePrototype>(crystallizer.SelectedRecipeId, out var recipe))
                return;

            float recipeTotalMoles = 0;
            foreach (var (gas, requiredMoles) in recipe.MinimumRequirements)
            {
                if (requiredMoles > 0 && crystalMix.GetMoles(gas) < requiredMoles * 2)
                {
                    recipeTotalMoles += pipeMix.GetMoles(gas);
                }
            }

            if (recipeTotalMoles <= 0)
                return;

            var buffer = crystallizer.InputBuffer;
            buffer.Clear();
            buffer.Temperature = pipeMix.Temperature;
            bool anyGasRemoved = false;

            foreach (var (gas, requiredMoles) in recipe.MinimumRequirements)
            {
                if (requiredMoles <= 0)
                    continue;

                if (crystalMix.GetMoles(gas) >= requiredMoles * 2)
                    continue;

                float availableMoles = pipeMix.GetMoles(gas);
                float proportion = availableMoles / recipeTotalMoles;
                proportion = Math.Min(proportion, 1.0f);

                float molesToRemove = crystallizer.GasInput * crystallizer.InputEfficiency * proportion;

                float molesRemoved = Math.Min(availableMoles, molesToRemove);
                if (molesRemoved > 0)
                {
                    pipeMix.AdjustMoles(gas, -molesRemoved);
                    buffer.AdjustMoles(gas, molesRemoved);
                    anyGasRemoved = true;
                }
            }

            if (anyGasRemoved)
            {
                _atmos.Merge(crystalMix, buffer);
            }
        }
    }

    private void ProcessRecipe(EntityUid uid, CrystallizerComponent crystallizer)
    {
        var crystalMix = crystallizer.CrystallizerGasMixture;
        if (string.IsNullOrEmpty(crystallizer.SelectedRecipeId) ||
            !_prototypeManager.TryIndex<CrystallizerRecipePrototype>(crystallizer.SelectedRecipeId, out var recipe) ||
            crystalMix.TotalMoles <= 0)
            return;

        bool gasCheck = CheckGasRequirements(crystalMix, recipe);
        bool tempCheck = CheckTempRequirements(crystalMix, recipe);

        if (gasCheck && tempCheck)
        {
            HeatCalculations(crystalMix, recipe, crystallizer);
            float progressFactor = recipe.ProgressRateMultiplier / ProgressDenominator(crystallizer.TotalRecipeMoles);
            crystallizer.ProgressBar = Math.Min(crystallizer.ProgressBar + MinProgressAmount * progressFactor, 100f);
        }
        else
        {
            crystallizer.QualityLoss = Math.Min(crystallizer.QualityLoss + StallQualityPenalty, 100f);
            crystallizer.ProgressBar = Math.Max(crystallizer.ProgressBar - 1f, 0f);
        }

        if (crystallizer.ProgressBar >= 100f)
        {
            CompleteRecipe(uid, crystallizer, recipe);
        }
        UpdateProgressBarUI(uid, crystallizer);
    }

    private bool CheckGasRequirements(GasMixture crystalMix, CrystallizerRecipePrototype recipe)
    {
        foreach (var (gas, requiredMoles) in recipe.MinimumRequirements)
        {
            if (requiredMoles > 0 && crystalMix.GetMoles(gas) < requiredMoles)
                return false;
        }
        return true;
    }

    private bool CheckTempRequirements(GasMixture crystalMix, CrystallizerRecipePrototype recipe)
    {
        float temperature = crystalMix.Temperature;
        return temperature >= recipe.MinimumTemperature * CrystallizerConstants.MinDeviationRate &&
               temperature <= recipe.MaximumTemperature * CrystallizerConstants.MaxDeviationRate;
    }

    private static float ProgressDenominator(float totalRecipeMoles)
    {
        return MathF.Max(MathF.Log10(totalRecipeMoles * RecipeMolesLogScale), 1f);
    }

    private void HeatCalculations(GasMixture crystalMix, CrystallizerRecipePrototype recipe, CrystallizerComponent crystallizer)
    {
        float progressAmountToQuality = MinProgressAmount * recipe.QualityRateMultiplier / ProgressDenominator(crystallizer.TotalRecipeMoles);
        float medianTemperature = (recipe.MaximumTemperature + recipe.MinimumTemperature) / 2f;

        if ((crystalMix.Temperature >= recipe.MinimumTemperature * CrystallizerConstants.MinDeviationRate && crystalMix.Temperature <= recipe.MinimumTemperature) ||
            (crystalMix.Temperature >= recipe.MaximumTemperature && crystalMix.Temperature <= recipe.MaximumTemperature * CrystallizerConstants.MaxDeviationRate))
        {
            crystallizer.QualityLoss = Math.Min(crystallizer.QualityLoss + progressAmountToQuality, 100f);
        }

        if (crystalMix.Temperature >= medianTemperature * CrystallizerConstants.MinDeviationRate && crystalMix.Temperature <= medianTemperature * CrystallizerConstants.MaxDeviationRate)
        {
            // Mono nerf: floor снижен с -85 до -25.
            crystallizer.QualityLoss = Math.Max(crystallizer.QualityLoss - progressAmountToQuality, -25f);
        }

        float heatCapacity = _atmos.GetHeatCapacity(crystalMix, true);
        if (heatCapacity > 0)
        {
            crystalMix.Temperature = Math.Max(crystalMix.Temperature + recipe.EnergyRelease / heatCapacity, Atmospherics.TCMB);
        }
    }

    private void CompleteRecipe(EntityUid uid, CrystallizerComponent crystallizer, CrystallizerRecipePrototype recipe)
    {
        var crystalMix = crystallizer.CrystallizerGasMixture;

        // Consume gases
        foreach (var (gas, requiredMoles) in recipe.MinimumRequirements)
        {
            if (requiredMoles <= 0)
                continue;

            float amountConsumed = requiredMoles + (requiredMoles * crystallizer.QualityLoss * 0.01f);
            float availableMoles = crystalMix.GetMoles(gas);
            if (availableMoles < amountConsumed)
            {
                crystallizer.QualityLoss = Math.Min(crystallizer.QualityLoss + 10f, 100f);
            }
            crystalMix.AdjustMoles(gas, -Math.Min(availableMoles, amountConsumed));
        }

        // Spawn products
        var mapCoords = _transform.GetMapCoordinates(uid);
        if (TryComp<TransformComponent>(uid, out var transform) && transform.MapID != MapId.Nullspace)
        {
            var gridUid = transform.GridUid;
            if (gridUid != null && EntityManager.TryGetComponent<MapGridComponent>(gridUid, out var mapGrid))
            {
                var tile = _map.CoordinatesToTile(gridUid.Value, mapGrid, mapCoords);
                var southTile = tile + new Vector2i(0, -1);
                var coords = _map.GridTileToLocal(gridUid.Value, mapGrid, southTile);
                foreach (var (productId, amount) in recipe.Products)
                {
                    for (int i = 0; i < amount; i++)
                    {
                        EntityManager.SpawnEntity(productId, coords);
                    }
                }
            }
        }

        // Reset
        crystallizer.ProgressBar = 0f;
        crystallizer.QualityLoss = 0f;
    }

    private float CalculateTotalRecipeMoles(CrystallizerComponent crystallizer)
    {
        if (string.IsNullOrEmpty(crystallizer.SelectedRecipeId) ||
            !_prototypeManager.TryIndex<CrystallizerRecipePrototype>(crystallizer.SelectedRecipeId, out var recipe))
            return 0f;

        float total = 0f;
        foreach (var moles in recipe.MinimumRequirements.Values)
        {
            total += moles;
        }
        return total;
    }

    private void ProcessTemperatureRegulator(EntityUid uid, CrystallizerComponent crystallizer, float dt)
    {
        var crystalMix = crystallizer.CrystallizerGasMixture;
        if (!GetRegulatorPipeMixture(uid, crystallizer, out var regulatorMix) || regulatorMix == null)
            return;

        if (regulatorMix.TotalMoles < 0.01 || crystalMix.TotalMoles <= 0)
            return;

        float regulatorHeatCapacity = _atmos.GetHeatCapacity(regulatorMix, true);
        float crystalHeatCapacity = _atmos.GetHeatCapacity(crystalMix, true);

        if (regulatorHeatCapacity < Atmospherics.MinimumHeatCapacity || crystalHeatCapacity < Atmospherics.MinimumHeatCapacity)
            return;

        float temperatureDelta = regulatorMix.Temperature - crystalMix.Temperature;

        const float conductivityRatio = 0.95f;
        float heatAmount = conductivityRatio * temperatureDelta * (regulatorHeatCapacity * crystalHeatCapacity / (regulatorHeatCapacity + crystalHeatCapacity));

        regulatorMix.Temperature = Math.Max(regulatorMix.Temperature - heatAmount / regulatorHeatCapacity, Atmospherics.TCMB);
        crystalMix.Temperature = Math.Max(crystalMix.Temperature + heatAmount / crystalHeatCapacity, Atmospherics.TCMB);
    }

    private bool GetInputPipeMixture(EntityUid uid, CrystallizerComponent crystallizer, out GasMixture? mixture)
    {
        mixture = null;
        if (!_nodeContainer.TryGetNode(uid, crystallizer.InletName, out PipeNode? inlet))
            return false;

        mixture = inlet.Air;
        return true;
    }

    private bool GetRegulatorPipeMixture(EntityUid uid, CrystallizerComponent crystallizer, out GasMixture? mixture)
    {
        mixture = null;
        if (!_nodeContainer.TryGetNode(uid, crystallizer.RegulatorName, out PipeNode? regulator))
            return false;

        mixture = regulator.Air;
        return true;
    }
}
