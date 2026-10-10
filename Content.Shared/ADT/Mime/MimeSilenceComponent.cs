using Robust.Shared.Prototypes;
namespace Content.Shared.ADT.Mime;

[RegisterComponent]
public sealed partial class MimeSilenceComponent : Component
{
    [DataField("silenceAction")]
    public EntProtoId SilenceAction = "ADTActionMimeSilence";

    [DataField("silenceActionEntity")]
    public EntityUid? SilenceActionEntity;
}
