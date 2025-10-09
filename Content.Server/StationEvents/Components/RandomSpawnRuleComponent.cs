using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Spawns entities at random tiles on a station. ADT-Tweak
/// </summary>
[RegisterComponent, Access(typeof(RandomSpawnRule))]
public sealed partial class RandomSpawnRuleComponent : Component
{
    /// <summary>
    /// The entity to be spawned.
    /// </summary>
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = string.Empty;


    /// <summary>
    /// Minimum number of entities to spawn | ADT-Europe-Port-Tweak
    /// </summary>
    [DataField]
    public int MinCount = 1;

    /// <summary>
    /// Maximum number of entities to spawn | ADT-Europe-Port-Tweak
    /// </summary>
    [DataField]
    public int MaxCount = 1;
}
