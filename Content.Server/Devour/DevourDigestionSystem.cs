using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Devour.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Timing;
using System;
using Robust.Shared.GameObjects;
using System.Linq;

using Robust.Shared.Containers;

namespace Content.Server.Devour;

/// <summary>
/// System for handling digestion of devoured prey and nutrient absorption
/// </summary>
public sealed class DevourDigestionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    private readonly TimeSpan _digestionInterval = TimeSpan.FromSeconds(5);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GutsComponent, MapInitEvent>(OnGutsInit);
        SubscribeLocalEvent<GutsComponent, ComponentStartup>(OnGutsStartup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<GutsComponent>();

        while (query.MoveNext(out var uid, out var guts))
        {
            if (guts.GutsContainer.ContainedEntities.Count == 0 && guts.CurrentAmount == 0)
                continue;

            // Process digestion every interval
            if (currentTime - guts.LastDigestionTime >= _digestionInterval)
            {
                ProcessDigestion(uid, guts);
                guts.LastDigestionTime = currentTime;
            }
        }
    }

    private void OnGutsInit(EntityUid uid, GutsComponent component, MapInitEvent args)
    {
        // Initialize the guts container
        component.GutsContainer = _containerSystem.EnsureContainer<Container>(uid, "guts");

        // Initialize the nutrient solution
        if (_solutionSystem.TryGetSolution(uid, "guts", out var solution))
        {
            component.NutrientSolution = solution;
        }
        else
        {
            _solutionSystem.EnsureSolutionEntity(uid, "guts", out var newSolution);
            component.NutrientSolution = newSolution;
        }
    }

    private void OnGutsStartup(EntityUid uid, GutsComponent component, ComponentStartup args)
    {
        component.LastDigestionTime = _gameTiming.CurTime;
    }

    private void ProcessDigestion(EntityUid uid, GutsComponent guts)
    {
        var digestedAmount = 0;

        // Process contained entities (devoured prey)
        var entitiesToProcess = guts.GutsContainer.ContainedEntities.ToArray();
        foreach (var entity in entitiesToProcess)
        {
            if (!TryComp<MobStateComponent>(entity, out var mobState))
                continue;

            // Check if prey can be digested
            if (!CanDigestPrey(entity, mobState))
                continue;

            // Digest the prey
            var currentDigestAmount = DigestPrey(entity, guts);
            digestedAmount += currentDigestAmount;

            // Show digestion progress messages
            ShowDigestionProgress(uid, entity, currentDigestAmount);

            // Remove prey if fully digested
            if (currentDigestAmount >= 100)
            {
                _containerSystem.Remove(entity, guts.GutsContainer);
                _popupSystem.PopupEntity(Loc.GetString("digestion-complete-predator", ("prey", Name(entity))), uid, uid);
                _popupSystem.PopupEntity(Loc.GetString("digestion-complete-prey", ("predator", Name(uid))), entity, entity);
                QueueDel(entity);
            }
        }

        // Process nutrients
        if (guts.NutrientSolution != null && digestedAmount > 0)
        {
            ProcessNutrients(uid, guts, digestedAmount);
        }

        // Update current amount
        guts.CurrentAmount = Math.Max(0, guts.CurrentAmount + digestedAmount);
    }

    private bool CanDigestPrey(EntityUid prey, MobStateComponent mobState)
    {
        if (!TryComp<PreyComponent>(prey, out var preyComp))
            return false;

        if (preyComp.Undigestible)
            return false;

        switch (mobState.CurrentState)
        {
            case MobState.Alive:
                return preyComp.DigestibleWhileAlive;
            case MobState.Critical:
            case MobState.Dead:
                return preyComp.DigestibleWhileDead;
            default:
                return false;
        }
    }

    private int DigestPrey(EntityUid prey, GutsComponent guts)
    {
        var digestAmount = 10; // Base digestion amount

        // Apply prey resistance
        if (TryComp<PreyComponent>(prey, out var preyComp))
        {
            digestAmount = (int)(digestAmount / preyComp.DigestionResistance);
        }

        // Apply predator digestion speed
        if (TryComp<PredatorComponent>(guts.Owner, out var predComp))
        {
            digestAmount = (int)(digestAmount * predComp.DigestionSpeedMultiplier);
        }

        return Math.Min(digestAmount, 100);
    }

    private void ProcessNutrients(EntityUid uid, GutsComponent guts, int digestedAmount)
    {
        if (guts.NutrientSolution == null)
            return;

        // Convert digested material to nutrients
        var nutrientAmount = digestedAmount * guts.NutrientAbsorptionRate;
        var wasteAmount = digestedAmount * guts.WasteProductionRate;

        // Add nutrients to bloodstream
        if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
        {
            var nutrientSolution = new Solution("Nutrients", FixedPoint2.New(nutrientAmount));
            _bloodstreamSystem.TryAddToChemicals(uid, nutrientSolution);
        }

        // Add waste to guts solution
        var wasteSolution = new Solution("Waste", FixedPoint2.New(wasteAmount));
        _solutionSystem.TryAddSolution(guts.NutrientSolution.Value, wasteSolution);
    }

    private void ShowDigestionProgress(EntityUid predator, EntityUid prey, int digestAmount)
    {
        // Show progress messages based on digestion amount
        if (digestAmount >= 50)
        {
            _popupSystem.PopupEntity(Loc.GetString("digestion-heavy-predator", ("prey", Name(prey))), predator, predator);
            _popupSystem.PopupEntity(Loc.GetString("digestion-heavy-prey", ("predator", Name(predator))), prey, prey);
        }
        else if (digestAmount >= 25)
        {
            _popupSystem.PopupEntity(Loc.GetString("digestion-moderate-predator", ("prey", Name(prey))), predator, predator);
            _popupSystem.PopupEntity(Loc.GetString("digestion-moderate-prey", ("predator", Name(predator))), prey, prey);
        }
        else if (digestAmount > 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("digestion-light-predator", ("prey", Name(prey))), predator, predator);
            _popupSystem.PopupEntity(Loc.GetString("digestion-light-prey", ("predator", Name(predator))), prey, prey);
        }
    }
}
