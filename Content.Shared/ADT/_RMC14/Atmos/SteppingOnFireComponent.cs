// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent]
public sealed partial class SteppingOnFireComponent : Component
{
    [ViewVariables]
    public float ArmorMultiplier = 1;

    [ViewVariables]
    public float Distance;

    [DataField]
    public TimeSpan UpdateTime = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan UpdateAt = TimeSpan.Zero;

    [ViewVariables]
    public EntityCoordinates? LastPosition;
}
