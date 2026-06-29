using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.ADT.Botany.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class PlantAnalyzerComponent : Component
{
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public EntityUid? ScannedEntity;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public DoAfterId? DoAfter;

    [DataField]
    public SoundSpecifier? ScanningEndSound;
}
