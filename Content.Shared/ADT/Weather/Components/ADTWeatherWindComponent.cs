using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weather.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTWeatherWindComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Speed = 1.8f;

    [DataField, AutoNetworkedField]
    public float Acceleration = 18f;

    [DataField, AutoNetworkedField]
    public Angle BaseDirection = Angle.Zero;

    [DataField, AutoNetworkedField]
    public Angle Spread = Angle.FromDegrees(55);

    [DataField, AutoNetworkedField]
    public float DirectionFrequency = 0.04f;

    [DataField, AutoNetworkedField]
    public float GustAmplitude = 0.35f;

    [DataField, AutoNetworkedField]
    public float GustFrequency = 0.22f;

    [DataField, AutoNetworkedField]
    public int Seed = 1;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
