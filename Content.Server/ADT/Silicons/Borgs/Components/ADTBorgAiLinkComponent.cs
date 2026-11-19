using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.ADT.Silicons.Borgs.Components;

[RegisterComponent]
public sealed partial class ADTBorgAiLinkComponent : Component
{
    [DataField]
    public bool AiLinkEnabled = true;

    [DataField]
    public bool LawSyncEnabled = true;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LawSyncFlashEnd;
}
