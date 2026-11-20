using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Vampire.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VampirePoolComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Hidden;

    [ViewVariables]
    public bool VisualHidden;

    [ViewVariables]
    public HashSet<EntityUid> Puddles = new();

    [DataField]
    public List<ProtoId<ReagentPrototype>> BloodReagents = new()
    {
        "Blood",
        "CopperBlood",
        "InsectBlood",
        "SulfurBlood",
        "AmmoniaBlood",
        "ResomiBlood",
    };

    [DataField]
    public string BodyLayer = "movement";

    [DataField]
    public string EffectLayer = "pool_fx";

    [DataField]
    public string MergeState = "pool_merge";

    [DataField]
    public string EmergeState = "pool_emerge";

    [DataField]
    public string HiddenState = "pool_hidden";

    [DataField]
    public TimeSpan TransitionDuration = TimeSpan.FromSeconds(0.49);
}
