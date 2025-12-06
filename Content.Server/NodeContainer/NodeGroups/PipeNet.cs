using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.ADT.Atmos.Piping.Components; // ADT-Tweak
using Content.Shared.Atmos;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;
using Content.Server.ADT.Atmos.Piping.Systems; // ADT-Tweak

namespace Content.Server.NodeContainer.NodeGroups
{
    public interface IPipeNet : INodeGroup, IGasMixtureHolder
    {
        /// <summary>
        ///     Causes gas in the PipeNet to react.
        /// </summary>
        void Update();
    }

    [NodeGroup(NodeGroupID.Pipe)]
    public sealed class PipeNet : BaseNodeGroup, IPipeNet
    {
        [ViewVariables] public GasMixture Air { get; set; } =
            new() { Temperature = Atmospherics.T20C };

        [ViewVariables] private AtmosphereSystem? _atmosphereSystem;
        [ViewVariables] private IEntityManager? _entMan; // ADT-Tweak
        [ViewVariables] private OverpressurePipeDamageSystem? _overpressureSystem; // ADT-Tweak

        public EntityUid? Grid { get; private set; }

        // ADT-Tweak start
        [ViewVariables] public readonly List<EntityUid> OverpressurePipeEntities = new();
        [ViewVariables] public float MinPressureLimit = float.MaxValue;
        // ADT-Tweak end

        public override void Initialize(Node sourceNode, IEntityManager entMan)
        {
            base.Initialize(sourceNode, entMan);

            Grid = entMan.GetComponent<TransformComponent>(sourceNode.Owner).GridUid;

            if (Grid == null)
            {
                // This is probably due to a cannister or something like that being spawned in space.
                return;
            }

            _entMan = entMan; // ADT-Tweak
            _atmosphereSystem = entMan.EntitySysManager.GetEntitySystem<AtmosphereSystem>();
            _overpressureSystem =
                entMan.EntitySysManager.GetEntitySystem<OverpressurePipeDamageSystem>(); // ADT-Tweak

            _atmosphereSystem.AddPipeNet(Grid.Value, this);
        }

        public void Update()
        {
            _atmosphereSystem?.React(Air, this);

            // ADT-Tweak start
            if (Air.Pressure <= MinPressureLimit || OverpressurePipeEntities.Count == 0)
                return;

            _overpressureSystem?.Update(this);
            // ADT-Tweak end
        }

        public override void LoadNodes(List<Node> groupNodes)
        {
            base.LoadNodes(groupNodes);

            // ADT-Tweak start
            OverpressurePipeEntities.Clear();
            MinPressureLimit = float.MaxValue;
            // ADT-Tweak end

            foreach (var node in groupNodes)
            {
                var pipeNode = (PipeNode) node;
                Air.Volume += pipeNode.Volume;

                // ADT-Tweak start
                if (_entMan != null && _entMan.HasComponent<OverpressurePipeDamageComponent>(pipeNode.Owner))
                {
                    var comp = _entMan.GetComponent<OverpressurePipeDamageComponent>(pipeNode.Owner);
                    OverpressurePipeEntities.Add(pipeNode.Owner);
                    MinPressureLimit = MathF.Min(MinPressureLimit, comp.LimitPressure);
                }
                // ADT-Tweak end
            }
        }

        public override void RemoveNode(Node node)
        {
            base.RemoveNode(node);

            // ADT-Tweak start
            if (node is PipeNode removedPipe)
                OverpressurePipeEntities.Remove(removedPipe.Owner);
            // ADT-Tweak end

            // if the node is simply being removed into a separate group, we do nothing,
            // as gas redistribution will be handled by AfterRemake().
            // But if it is being deleted, we actually want to remove the gas stored in this node.
            if (!node.Deleting || node is not PipeNode pipe)
                return;

            Air.Multiply(1f - pipe.Volume / Air.Volume);
            Air.Volume -= pipe.Volume;
        }

        public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
        {
            RemoveFromGridAtmos();

            var newAir = new List<GasMixture>(newGroups.Count());
            foreach (var newGroup in newGroups)
            {
                if (newGroup.Key is IPipeNet newPipeNet)
                    newAir.Add(newPipeNet.Air);
            }

            _atmosphereSystem?.DivideInto(Air, newAir);
        }

        private void RemoveFromGridAtmos()
        {
            if (Grid == null)
                return;

            _atmosphereSystem?.RemovePipeNet(Grid.Value, this);
        }

        public override string GetDebugData()
        {
            return @$"Pressure: {Air.Pressure:G3}
Temperature: {Air.Temperature:G3}
Volume: {Air.Volume:G3}";
        }
    }
}
