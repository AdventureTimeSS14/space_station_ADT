using Content.Server.Administration.Logs;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.ADT.Chemistry;
using Content.Server.ADT.Chemistry.Components;

namespace Content.Server.ADT.Chemistry.EntitySystems
{
    /// <summary>
    /// Contains all the server-side logic for reagent analyzer.
    /// <seealso cref="ReagentAnalyzerComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ReagentAnalyzerSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentAnalyzerComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentAnalyzerComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentAnalyzerComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentAnalyzerComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentAnalyzerComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<ReagentAnalyzerComponent, ReagentAnalyzerClearContainerSolutionMessage>(OnClearContainerSolutionMessage);
        }

        private void SubscribeUpdateUiState<T>(Entity<ReagentAnalyzerComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
        }

        private void UpdateUiState(Entity<ReagentAnalyzerComponent> reagentAnalyzer)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentAnalyzer, SharedReagentAnalyzer.OutputSlotName);
            var outputContainerInfo = BuildOutputContainerInfo(outputContainer);

            var state = new ReagentAnalyzerBoundUserInterfaceState(outputContainerInfo);
            _userInterfaceSystem.SetUiState(reagentAnalyzer.Owner, ReagentAnalyzerUiKey.Key, state);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out _, out var solution))
            {
                return new ContainerInfo(Name(container.Value), solution.Volume, solution.MaxVolume)
                {
                    Reagents = solution.Contents
                };
            }

            return null;
        }


        private void OnClearContainerSolutionMessage(Entity<ReagentAnalyzerComponent> reagentAnalyzer, ref ReagentAnalyzerClearContainerSolutionMessage message)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentAnalyzer, SharedReagentAnalyzer.OutputSlotName);
            if (outputContainer is not { Valid: true } || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
                return;

            _solutionContainerSystem.RemoveAllSolution(solution.Value);
            UpdateUiState(reagentAnalyzer);
            ClickSound(reagentAnalyzer);
        }

        private void ClickSound(Entity<ReagentAnalyzerComponent> reagentAnalyzer)
        {
            _audioSystem.PlayPvs(reagentAnalyzer.Comp.ClickSound, reagentAnalyzer, AudioParams.Default.WithVolume(-2f));
        }
    }
}
