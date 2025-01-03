using Content.Server.NPC.Systems;
using Content.Shared.Actions;
using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Components;

// ADT: Компонент был полностью переписан, заменяйте при апстриме на нашу версию.

/// <summary>
/// This is used for an NPC that constantly tries to use an action on a given target.
/// </summary>
[RegisterComponent, Access(typeof(NPCUseActionOnTargetSystem))]
public sealed partial class NPCUseActionOnTargetComponent : Component
{
    /// <summary>
    /// HTN blackboard key for the target entity
    /// </summary>
    [DataField]
    public string TargetKey = "Target";

    // /// <summary>
    // /// Action that's going to attempt to be used.
    // /// </summary>
    // [DataField(required: true)]
    // public EntProtoId<EntityWorldTargetActionComponent> ActionId;

    // [DataField]
    // public EntityUid? ActionEnt;

    [DataField(required: true)]
    public ProtoId<WeightedRandomEntityPrototype> Actions = "GoliathActions";

    [ViewVariables]
    public Dictionary<EntityUid, float> ActionEntities = new();

    [DataField]
    public float Delay = 7f;

    [ViewVariables]
    public TimeSpan LastAction = TimeSpan.Zero;
}
