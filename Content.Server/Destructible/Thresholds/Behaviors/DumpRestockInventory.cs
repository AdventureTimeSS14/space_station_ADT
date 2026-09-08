using System.Linq;
using Robust.Shared.Random;
using Content.Shared.ADT.VendingMachines;
using Content.Shared.Stacks;
using Content.Shared.Prototypes;
using Content.Shared.VendingMachines;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Spawns a random amount of items from one of the canRestock
    ///     inventory entries on a VendingMachineRestock component.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed partial class DumpRestockInventory: IThresholdBehavior
    {
        /// ADT-Tweak start
        /// <summary>
        ///     The percent of each inventory entry that will be salvaged
        ///     upon destruction of the package.
        /// </summary>
        ///[DataField("percent", required: true)]
        ///public float Percent = 0.5f;

        [DataField("minCount")]
        public int MinCount = 2;

        [DataField("maxCount")]
        public int MaxCount = 5;
        // ADT-Tweak end
        [DataField("offset")]
        public float Offset { get; set; } = 0.5f;

        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            if (!system.EntityManager.TryGetComponent<VendingMachineRestockComponent>(owner, out var packagecomp) ||
                !system.EntityManager.TryGetComponent<TransformComponent>(owner, out var xform))
                return;

            var randomInventory = system.Random.Pick(packagecomp.CanRestock);

            if (!system.PrototypeManager.TryIndex(randomInventory, out VendingMachineInventoryPrototype? packPrototype))
                return;

            // ADT-Tweak start
            var inventory = VendingMachineInventoryData.Flatten(packPrototype.StartingInventory).ToList(); // ADT-Tweak
            if (inventory.Count == 0)
                return;

            var count = system.Random.Next(MinCount, MaxCount + 1);
            for (var i = 0; i < count; i++)
            {
                var (entityId, _, _) = system.Random.Pick(inventory);
            // ADT-Tweak end

                if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId, system.PrototypeManager, system.EntityManager.ComponentFactory))
                {
                    var spawned = system.EntityManager.SpawnEntity(entityId, xform.Coordinates.Offset(system.Random.NextVector2(-Offset, Offset)));
                    system.StackSystem.SetCount((spawned, null), 1); // ADT-Tweak
                    system.EntityManager.GetComponent<TransformComponent>(spawned).LocalRotation = system.Random.NextAngle();
                }
                else
                {
                    var spawned = system.EntityManager.SpawnEntity(entityId, xform.Coordinates.Offset(system.Random.NextVector2(-Offset, Offset)));
                    system.EntityManager.GetComponent<TransformComponent>(spawned).LocalRotation = system.Random.NextAngle();
                }
            }
        }
    }
}