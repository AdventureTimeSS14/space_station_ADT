using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Objectives.Components;

[RegisterComponent, Access(typeof(Systems.AmassCreditsConditionSystem))]
public sealed partial class AmassCreditsConditionComponent : Component
{
    [DataField(required: true)]
    public int TargetCredits;

    [DataField(required: true)]
    public LocId ObjectiveText;

    [DataField(required: true)]
    public LocId DescriptionText;
}
