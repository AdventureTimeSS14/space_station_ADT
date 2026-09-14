using Robust.Shared.GameStates;

namespace Content.Shared.ADT.DrunkDrift;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTDrunkDriftComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool VisualsActive;

    [DataField]
    public TimeSpan VisualThreshold = TimeSpan.FromSeconds(50);

    [DataField, AutoNetworkedField]
    public float SwayAmplitude = 0.12f;

    [DataField, AutoNetworkedField]
    public float LurchChance = 0.35f;

    [DataField, AutoNetworkedField]
    public float LurchAngle = 0.25f;

    [DataField, AutoNetworkedField]
    public float LurchInterval = 3f;
}
