using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Radio.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ADTTunableRadioComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Frequency = 1330;

    [DataField, AutoNetworkedField]
    public int MinFrequency = 1200;

    [DataField, AutoNetworkedField]
    public int MaxFrequency = 1600;

    [DataField, AutoNetworkedField]
    public bool Locked;

    [DataField, AutoNetworkedField]
    public bool MicrophoneEnabled;

    [DataField, AutoNetworkedField]
    public bool SpeakerEnabled = true;

    [DataField]
    public int ListenRange = 4;

    [DataField]
    public bool CrossMap = true;

    [DataField]
    public bool Loud;

    [DataField]
    public Color ChatColor = Color.FromHex("#967101");

    [DataField]
    public string? Effect = "walkie_talkie";

    [DataField]
    public SoundSpecifier? SoundOnReceive = new SoundPathSpecifier("/Audio/ADT/Effects/static.ogg")
    {
        Params = AudioParams.Default.WithVolume(-8f).WithMaxDistance(6f).WithVariation(0.08f),
    };

    [DataField]
    public SoundSpecifier? SoundOnTune = new SoundPathSpecifier("/Audio/Machines/button.ogg")
    {
        Params = AudioParams.Default.WithVolume(-6f).WithMaxDistance(4f).WithVariation(0.12f),
    };

    [DataField]
    public SoundSpecifier? SoundOnToggle = new SoundPathSpecifier("/Audio/Machines/quickbeep.ogg")
    {
        Params = AudioParams.Default.WithVolume(-8f).WithMaxDistance(4f),
    };
}
