using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Sandevistan;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SandevistanSlowedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Source;

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public Vector2 OriginalLinearVelocity;

    [DataField, AutoNetworkedField]
    public bool IsSlowed = true;

    [DataField, AutoNetworkedField]
    public bool HadGlitch;
}
