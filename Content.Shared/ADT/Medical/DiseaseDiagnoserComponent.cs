using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Medical;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DiseaseDiagnoserComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string ContainerId = "swab";

    [DataField, AutoNetworkedField]
    public bool Working;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan WorkDuration;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan WorkTimeEnd;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? WorkingSound;

    [ViewVariables]
    public Entity<AudioComponent>? WorkingSoundEntity;
}

[Serializable, NetSerializable]
public enum DiseaseDiagnoserVisuals : byte
{
    Printing
}