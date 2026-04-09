using Content.Shared.StatusIcon;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Radio.Components;

[RegisterComponent]
public sealed partial class RadioJobIconOverrideComponent : Component
{
    [DataField(required: true)]
    public ProtoId<JobIconPrototype> IconId = "JobIconNoId";

    [DataField]
    public string? JobNameOverride;
}
