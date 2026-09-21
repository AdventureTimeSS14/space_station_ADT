using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.GPS;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class GpsComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Tag = "MINE0";

    [DataField]
    public int MaxTagLength = 5;

    [DataField, AutoNetworkedField]
    public bool Tracking;

    [DataField]
    public bool CanToggle = true;

    [DataField, AutoNetworkedField]
    public bool SameMapOnly;

    [DataField]
    public float UpdateRate = 1f;

    [ViewVariables]
    public TimeSpan NextUpdate;

    [DataField, AutoNetworkedField]
    public bool Sos;

    [DataField]
    public bool SosOnDeath = true;

    [DataField]
    public TimeSpan SosCooldown = TimeSpan.FromMinutes(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextSosToggle;

    [DataField]
    public SoundSpecifier? SosSound = new SoundPathSpecifier("/Audio/ADT/Machines/gps_sos.ogg");
}
