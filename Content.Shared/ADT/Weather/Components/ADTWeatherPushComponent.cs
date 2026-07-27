using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weather.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTWeatherPushComponent : Component
{
    [DataField]
    public float Strength = 2.5f;

    [DataField]
    public float Speed = 6f;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public TimeSpan DirectionChangeMin = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan DirectionChangeMax = TimeSpan.FromSeconds(30);

    [ViewVariables]
    public Angle Direction;

    [ViewVariables]
    public TimeSpan NextDirectionChange;
}
