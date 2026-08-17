using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.ADT.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Atmos.EntitySystems;
public sealed class GasEvaporatorSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly Dictionary<ProtoId<ReagentPrototype>, int> _gasIndexByReagent = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasEvaporatorComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
        SubscribeLocalEvent<GasEvaporatorComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<GasEvaporatorComponent, ExaminedEvent>(OnExamine);
    }

    private void OnAtmosUpdate(Entity<GasEvaporatorComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        if (entity.Comp.Mode != GasCondenserMode.Evaporate
            || !_power.IsPowered(entity.Owner)
            || !_nodeContainer.TryGetNode(entity.Owner, entity.Comp.Inlet, out PipeNode? inlet)
            || !TryGetBeakerSolution(entity, out var solEnt, out var solution))
        {
            return;
        }

        var amount = FixedPoint2.Min(FixedPoint2.New(entity.Comp.UnitsPerSecond * args.dt), solution.Volume);
        if (amount <= 0)
            return;

        EnsureGasMap();

        var converted = FixedPoint2.Zero;
        foreach (var reagentQuantity in solution.Contents.ToArray())
        {
            if (!_gasIndexByReagent.TryGetValue(reagentQuantity.Reagent.Prototype, out var gasIndex))
                continue;

            var toConvert = FixedPoint2.Min(amount - converted, reagentQuantity.Quantity);
            if (toConvert <= 0)
                continue;

            inlet.Air.AdjustMoles(gasIndex, toConvert.Float() * entity.Comp.MolesToGasMultiplier);
            _solution.RemoveReagent(solEnt.Value, reagentQuantity.Reagent, toConvert);
            converted += toConvert;
        }

        if (converted > 0)
            _solution.UpdateChemicals(solEnt.Value);
    }

    private void OnGetVerbs(Entity<GasEvaporatorComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("gas-condenser-toggle-mode"),
            Act = () => ToggleMode(entity),
            Priority = 10
        });
    }

    private void OnExamine(Entity<GasEvaporatorComponent> entity, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(entity.Comp.Mode == GasCondenserMode.Condense
            ? "gas-condenser-examine-mode-condense"
            : "gas-condenser-examine-mode-evaporate"));

        if (_itemSlots.GetItemOrNull(entity, GasEvaporatorComponent.BeakerSlotId) is { } beaker
            && _solution.TryGetFitsInDispenser(beaker, out _, out var solution))
        {
            args.PushMarkup(Loc.GetString("gas-condenser-examine-beaker-present",
                ("volume", solution.Volume),
                ("maxVolume", solution.MaxVolume)));
        }
        else
        {
            args.PushMarkup(Loc.GetString("gas-condenser-examine-beaker-empty"));
        }

    }

    private void ToggleMode(Entity<GasEvaporatorComponent> entity)
    {
        entity.Comp.Mode = entity.Comp.Mode == GasCondenserMode.Condense
            ? GasCondenserMode.Evaporate
            : GasCondenserMode.Condense;
        Dirty(entity);

        var msg = Loc.GetString(entity.Comp.Mode == GasCondenserMode.Condense
            ? "gas-condenser-switched-condense"
            : "gas-condenser-switched-evaporate");
        _popup.PopupEntity(msg, entity, PopupType.Medium);
        _audio.PlayPvs(entity.Comp.SwitchSound, entity);
    }

    public bool IsInEvaporateMode(EntityUid uid)
    {
        return TryComp<GasEvaporatorComponent>(uid, out var comp)
            && comp.Mode == GasCondenserMode.Evaporate;
    }
    public bool TryGetOutputSolution(EntityUid uid, string solutionId, [NotNullWhen(true)] ref Entity<SolutionComponent>? cached,
        [NotNullWhen(true)] out Solution? solution)
    {
        solution = null;

        if (_itemSlots.GetItemOrNull(uid, GasEvaporatorComponent.BeakerSlotId) is { } beaker
            && _solution.TryGetFitsInDispenser(beaker, out var solEnt, out var beakerSolution))
        {
            cached = solEnt;
            solution = beakerSolution;
            return true;
        }

        cached = null;
        return _solution.ResolveSolution(uid, solutionId, ref cached, out solution);
    }

    private bool TryGetBeakerSolution(Entity<GasEvaporatorComponent> entity,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solEnt,
        [NotNullWhen(true)] out Solution? solution)
    {
        solEnt = null;
        solution = null;

        if (_itemSlots.GetItemOrNull(entity, GasEvaporatorComponent.BeakerSlotId) is not { } beaker)
            return false;

        return _solution.TryGetFitsInDispenser(beaker, out solEnt, out solution);
    }

    private void EnsureGasMap()
    {
        if (_gasIndexByReagent.Count > 0)
            return;

        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var gas = _atmosphere.GetGas(i);
            if (gas.Reagent is { } reagent)
                _gasIndexByReagent[reagent] = i;
        }
    }
}
