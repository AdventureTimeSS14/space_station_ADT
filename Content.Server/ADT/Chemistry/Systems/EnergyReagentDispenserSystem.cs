using System.Linq;
using Content.Server.ADT.Chemistry.Components;
using Content.Shared.ADT.Chemistry;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Power.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Power.Components;
using Content.Shared.Emag.Systems;

namespace Content.Server.ADT.Chemistry.EntitySystems
{
    /// <summary>
    /// Contains all the server-side logic for reagent dispensers.
    /// <seealso cref="EnergyReagentDispenserComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class EnergyReagentDispenserSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly BatterySystem _battery = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
        [Dependency] private readonly ConstructionSystem _construction = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EnergyReagentDispenserComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<EnergyReagentDispenserComponent, EnergyReagentDispenserSetDispenseAmountMessage>(OnSetDispenseAmountMessage);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, EnergyReagentDispenserDispenseReagentMessage>(OnDispenseReagentMessage);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, EnergyReagentDispenserClearContainerSolutionMessage>(OnClearContainerSolutionMessage);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, GotEmaggedEvent>(OnEmaged);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, RefreshPartsEvent>(OnPartsRefresh);
            SubscribeLocalEvent<EnergyReagentDispenserComponent, UpgradeExamineEvent>(OnUpgradeExamine);

            SubscribeLocalEvent<EnergyReagentDispenserComponent, MapInitEvent>(OnMapInit, before: [typeof(ItemSlotsSystem)]);
        }

        private void SubscribeUpdateUiState<T>(Entity<EnergyReagentDispenserComponent> ent, ref T ev) =>
            UpdateUiState(ent);

        private void OnEntInserted(Entity<EnergyReagentDispenserComponent> ent, ref EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID == EnergyReagentDispenserComponent.PartContainerName)
                SyncBatteryFromCell(ent);

            UpdateUiState(ent);
        }

        private void OnEntRemoved(Entity<EnergyReagentDispenserComponent> ent, ref EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID == EnergyReagentDispenserComponent.PartContainerName)
            {
                SyncCellFromBattery(ent, args.Entity);

                if (TryComp<BatteryComponent>(ent, out var battery))
                {
                    _battery.SetCharge((ent, battery), 0f);
                    _battery.SetMaxCharge((ent, battery), 1500f);
                }
            }

            UpdateUiState(ent);
        }

        private EntityUid? GetCellInParts(Entity<EnergyReagentDispenserComponent> ent)
        {
            if (!_container.TryGetContainer(ent.Owner, EnergyReagentDispenserComponent.PartContainerName, out var partContainer))
                return null;

            foreach (var item in partContainer.ContainedEntities)
            {
                if (HasComp<BatteryComponent>(item))
                    return item;
            }

            return null;
        }

        private void SyncBatteryFromCell(Entity<EnergyReagentDispenserComponent> ent)
        {
            if (!TryComp<BatteryComponent>(ent, out var battery))
                return;

            var cell = GetCellInParts(ent);
            if (cell == null || !TryComp<BatteryComponent>(cell.Value, out var cellBattery))
                return;

            _battery.SetMaxCharge((ent, battery), cellBattery.MaxCharge);
            var charge = _battery.GetCharge((cell.Value, cellBattery));
            _battery.SetCharge((ent, battery), Math.Min(charge, cellBattery.MaxCharge));
        }

        private void SyncCellFromBattery(Entity<EnergyReagentDispenserComponent> ent, EntityUid cell)
        {
            if (!TryComp<BatteryComponent>(cell, out var cellBattery)
                || !TryComp<BatteryComponent>(ent, out var battery))
                return;

            var charge = _battery.GetCharge((ent, battery));
            _battery.SetCharge((cell, cellBattery), Math.Min(charge, cellBattery.MaxCharge));
        }

        private void UpdateUiState(Entity<EnergyReagentDispenserComponent> reagentDispenser)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedEnergyReagentDispenser.OutputSlotName);
            var outputContainerInfo = BuildOutputContainerInfo(outputContainer);
            var inventory = GetInventory(reagentDispenser.Comp);

            var batteryCharge = 0f;
            var batteryMaxCharge = 0f;
            if (TryComp<BatteryComponent>(reagentDispenser, out var battery))
            {
                batteryCharge = _battery.GetCharge((reagentDispenser.Owner, battery));
                batteryMaxCharge = battery.MaxCharge;
            }

            var currentReceivingEnergy = 0f;
            if (TryComp<ApcPowerReceiverBatteryComponent>(reagentDispenser, out var apcPower))
                currentReceivingEnergy = apcPower.BatteryRechargeRate;

            var hasCell = reagentDispenser.Comp.InfiniteBattery || GetCellInParts(reagentDispenser) != null;

            if (!hasCell)
            {
                batteryCharge = 0f;
                batteryMaxCharge = 0f;
            }

            var state = new EnergyReagentDispenserBoundUserInterfaceState(
                outputContainerInfo,
                GetNetEntity(outputContainer),
                inventory,
                reagentDispenser.Comp.DispenseAmount,
                batteryCharge,
                batteryMaxCharge,
                currentReceivingEnergy,
                hasCell
            );
            _userInterfaceSystem.SetUiState(reagentDispenser.Owner, EnergyReagentDispenserUiKey.Key, state);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out _, out var solution))
            {
                return new ContainerInfo(Name(container.Value), solution.Volume, solution.MaxVolume)
                {
                    Reagents = solution.Contents,
                };
            }

            return null;
        }

        private List<EnergyReagentInventoryItem> GetInventory(EnergyReagentDispenserComponent comp)
        {
            var inventory = new List<EnergyReagentInventoryItem>();

            foreach (var (reagentId, cost) in comp.Reagents)
            {
                if (!_prototypeManager.TryIndex<ReagentPrototype>(reagentId, out var reagentProto))
                    continue;

                var displayCost = cost * comp.FinalEnergyCostMultiplier;

                inventory.Add(new EnergyReagentInventoryItem(
                    reagentId,
                    reagentProto.LocalizedName,
                    displayCost,
                    reagentProto.SubstanceColor
                ));
            }

            inventory.Sort((a, b) => string.Compare(a.ReagentLabel, b.ReagentLabel, StringComparison.Ordinal));
            return inventory;
        }

        private void OnSetDispenseAmountMessage(Entity<EnergyReagentDispenserComponent> reagentDispenser, ref EnergyReagentDispenserSetDispenseAmountMessage message)
        {
            reagentDispenser.Comp.DispenseAmount = message.EnergyReagentDispenserDispenseAmount;
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnPowerChanged(Entity<EnergyReagentDispenserComponent> reagentDispenser, ref PowerChangedEvent args) =>
            UpdateUiState(reagentDispenser);

        private void OnDispenseReagentMessage(Entity<EnergyReagentDispenserComponent> reagentDispenser, ref EnergyReagentDispenserDispenseReagentMessage message)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedEnergyReagentDispenser.OutputSlotName);
            if (outputContainer is not { Valid: true }
                || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
                return;

            if (!TryComp<BatteryComponent>(reagentDispenser, out var battery))
                return;

            if (!reagentDispenser.Comp.InfiniteBattery
                && _container.TryGetContainer(reagentDispenser.Owner, EnergyReagentDispenserComponent.PartContainerName, out _)
                && GetCellInParts(reagentDispenser) == null)
            {
                _audioSystem.PlayPvs(reagentDispenser.Comp.PowerSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));
                return;
            }

            var amount = (int)reagentDispenser.Comp.DispenseAmount;
            var powerRequired = GetPowerCostForReagent(message.ReagentId, amount, reagentDispenser.Comp);

            if (_battery.GetCharge((reagentDispenser.Owner, battery)) < powerRequired)
            {
                _audioSystem.PlayPvs(reagentDispenser.Comp.PowerSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));
                return;
            }


            var sol = new Solution(message.ReagentId, amount);
            if (!_solutionContainerSystem.TryAddSolution(solution.Value, sol))
                return;

            if (!reagentDispenser.Comp.InfiniteBattery)
                _battery.ChangeCharge(reagentDispenser.Owner, -powerRequired);
            else
                _battery.SetCharge(reagentDispenser.Owner, battery.MaxCharge);
            ClickSound(reagentDispenser);
            UpdateUiState(reagentDispenser);
        }

        private void OnClearContainerSolutionMessage(Entity<EnergyReagentDispenserComponent> reagentDispenser, ref EnergyReagentDispenserClearContainerSolutionMessage message)
        {
            var outputContainerNullable = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedEnergyReagentDispenser.OutputSlotName);
            if (outputContainerNullable is not { Valid: true } outputContainer
                || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer, out var solution, out var soln))
                return;

            var refundedPower = soln.Sum(reagent => GetRefundPowerForReagent(reagent.Reagent.Prototype, (int)reagent.Quantity, reagentDispenser));
            if (refundedPower > 0)
                _battery.ChangeCharge(reagentDispenser.Owner, refundedPower);

            _solutionContainerSystem.RemoveAllSolution(solution.Value);
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void ClickSound(Entity<EnergyReagentDispenserComponent> reagentDispenser) =>
            _audioSystem.PlayPvs(reagentDispenser.Comp.ClickSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));

        private static float GetPowerCostForReagent(string reagentId, int amount, EnergyReagentDispenserComponent comp)
        {
            if (comp.Reagents.TryGetValue(reagentId, out var cost))
                return cost * amount * comp.FinalEnergyCostMultiplier;
            return float.MaxValue;
        }

        private static float GetRefundPowerForReagent(string reagentId, int amount, EnergyReagentDispenserComponent comp)
        {
            if (comp.Reagents.TryGetValue(reagentId, out var cost))
                return cost * amount * comp.FinalEnergyCostMultiplier;
            return 0f;
        }

        private void OnPartsRefresh(EntityUid uid, EnergyReagentDispenserComponent component, RefreshPartsEvent args)
        {
            component.FinalRechargeRate = component.RechargeRatePerTier * args.GetPartRating(MachinePartIds.Capacitor, 1f);

            var scanningModuleTier = args.GetPartRating(MachinePartIds.ScanningModule, 1f);
            foreach (var (tier, reagents) in component.TierReagents)
            {
                if (scanningModuleTier < tier)
                    continue;

                foreach (var reagent in reagents)
                    component.Reagents.TryAdd(reagent, component.TierReagentCost);
            }
            component.FinalEnergyCostMultiplier = args.GetStatMultiplier(MachineStat.EnergyCost);

            if (TryComp<ApcPowerReceiverBatteryComponent>(uid, out var apcBattery))
                _powerReceiver.SetBatteryRechargeRate(uid, component.FinalRechargeRate, apcBattery);

            SyncBatteryFromCell((uid, component));

            UpdateUiState((uid, component));
        }

        private static void OnUpgradeExamine(EntityUid uid, EnergyReagentDispenserComponent component, UpgradeExamineEvent args)
        {
            var rechargeMultiplier = component.FinalRechargeRate > 0f
                ? component.FinalRechargeRate / component.RechargeRatePerTier
                : 1f;

            args.AddPercentageUpgrade("machine-upgrade-charging-speed", rechargeMultiplier, benefit: true);
            args.AddPercentageUpgrade("machine-upgrade-energy-cost", component.FinalEnergyCostMultiplier, benefit: false);
        }

        private void OnMapInit(Entity<EnergyReagentDispenserComponent> entity, ref MapInitEvent args)
        {
            _itemSlotsSystem.AddItemSlot(entity.Owner, SharedEnergyReagentDispenser.OutputSlotName, entity.Comp.EnergyBeakerSlot);

            SyncBatteryFromCell(entity);

            if (TryComp<MachineComponent>(entity, out var machine))
                _construction.RefreshParts(entity.Owner, machine);
        }
        private void OnEmaged(Entity<EnergyReagentDispenserComponent> ent, ref GotEmaggedEvent args)
        {
            if (ent.Comp.ReagentsEmagged == null || ent.Comp.Emagged)
                return;
            foreach (var (name, price) in ent.Comp.ReagentsEmagged)
            {
                ent.Comp.Reagents.Add(name, price);
            }
            args.Handled = true;
            ent.Comp.Emagged = true;
            UpdateUiState(ent);
        }

    }
}
