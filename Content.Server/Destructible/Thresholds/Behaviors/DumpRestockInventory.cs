using System.Linq;
using Robust.Shared.Random;
using Content.Shared.ADT.VendingMachines; // ADT-Tweak
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Stacks;
<<<<<<< HEAD
using Content.Shared.Prototypes;
using Content.Shared.VendingMachines; // ADT-Tweak
=======
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
>>>>>>> wizards-filtered

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Spawns a portion of the total items from one of the canRestock
///     inventory entries on a VendingMachineRestock component.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class DumpRestockInventory : IThresholdBehavior
{
    /// <summary>
<<<<<<< HEAD
    ///     Spawns a random amount of items from one of the canRestock
    ///     inventory entries on a VendingMachineRestock component.
=======
    ///     The percent of each inventory entry that will be salvaged
    ///     upon destruction of the package.
>>>>>>> wizards-filtered
    /// </summary>
    [DataField(required: true)]
    public float Percent = 0.5f;

    [DataField]
    public float Offset { get; set; } = 0.5f;

    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
<<<<<<< HEAD
        /// ADT-Tweak start
        /// <summary>
        ///     The percent of each inventory entry that will be salvaged
        ///     upon destruction of the package.
        /// </summary>
        ///[DataField("percent", required: true)]
        ///public float Percent = 0.5f;

        [DataField("count")]
        public MinMax Count = new(2, 5);
        // ADT-Tweak end
        [DataField("offset")]
        public float Offset { get; set; } = 0.5f;
=======
        if (!system.EntityManager.TryGetComponent<VendingMachineRestockComponent>(owner, out var packagecomp) ||
            !system.EntityManager.TryGetComponent<TransformComponent>(owner, out var xform))
            return;

        var randomInventory = system.Random.Pick(packagecomp.CanRestock);
>>>>>>> wizards-filtered

        if (!system.PrototypeManager.TryIndex(randomInventory, out VendingMachineInventoryPrototype? packPrototype))
            return;

        foreach (var (entityId, count) in packPrototype.StartingInventory)
        {
            var toSpawn = (int)Math.Round(count * Percent);

            if (toSpawn == 0) continue;

<<<<<<< HEAD
            if (!system.PrototypeManager.TryIndex(randomInventory, out VendingMachineInventoryPrototype? packPrototype))
                return;

            // ADT-Tweak start
            var inventory = VendingMachineInventoryData.Flatten(packPrototype.StartingInventory).ToList(); // ADT-Tweak
            if (inventory.Count == 0)
                return;

            var count = Count.Next(system.Random);
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
=======
            if (system.PrototypeManager.TryIndex(entityId, out var entProto)
                && entProto.HasComp<StackComponent>(system.EntityManager.ComponentFactory))
            {
                var spawned = system.EntityManager.SpawnAttachedTo(entityId, xform.Coordinates.Offset(system.Random.NextVector2(-Offset, Offset)), rotation: system.Random.NextAngle());
                system.StackSystem.SetCount((spawned, null), toSpawn);
            }
            else
            {
                for (var i = 0; i < toSpawn; i++)
                    system.EntityManager.SpawnAttachedTo(entityId, xform.Coordinates.Offset(system.Random.NextVector2(-Offset, Offset)), rotation: system.Random.NextAngle());
>>>>>>> wizards-filtered
            }
        }
    }
}
