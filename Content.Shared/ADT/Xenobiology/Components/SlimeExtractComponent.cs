using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.Xenobiology;

namespace Content.Shared.ADT.Xenobiology.Components;

/// <summary>
/// A slime extract. Reacts to reagents added to its solution,
/// firing the configured <see cref="ExtractReactionPrototype"/>s.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlimeExtractComponent : Component
{
    /// <summary>
    /// The reactions this extract can perform. Each entry is a reagent reaction,
    /// consisting of the requirements and then the response.
    /// </summary>
    [DataField("extractReactions")]
    public List<ProtoId<ExtractReactionPrototype>> ExtractReactions = new();

    /// <summary>
    /// The name of the container that holds the solution.
    /// Needed so that the slime extract can communicate with the container itself.
    /// </summary>
    [DataField("containerName", required: true)]
    public string ContainerName = string.Empty;

    /// <summary>
    /// How many times this extract can be used before being exhausted.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int RemainingUses = 1;
}

/// <summary>
/// Tracks which extract reactions are currently pending on an extract.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlimeExtractActiveReactionComponent : Component
{
    /// <summary>
    /// Whether the current slime extract is paused.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool CurrentlyPaused = false;

    /// <summary>
    /// The reactions currently active on this extract, along with the timestamps of their activation.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<ProtoId<ExtractReactionPrototype>, TimeSpan> ActiveReactions = new();
}
