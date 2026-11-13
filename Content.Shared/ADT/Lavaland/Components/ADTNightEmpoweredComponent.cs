using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTNightEmpoweredComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MeleeDamageMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float IncomingDamageMultiplier = 1f;
}
