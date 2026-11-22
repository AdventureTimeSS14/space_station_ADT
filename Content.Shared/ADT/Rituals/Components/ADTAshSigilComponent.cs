using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Rituals;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTAshSigilComponent : Component
{
    [DataField]
    public EntProtoId Rune = "ADTAshRune";

    [DataField]
    public EntProtoId? ActivationEffect = "ADTAshRuneActivation";

    [DataField]
    public TimeSpan ActivationDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan? ActivateAt;

    [DataField]
    public float MarkRange = 3f;

    [DataField, AutoNetworkedField]
    public bool Transforming;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ADTAshRuneMarkComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Lit;
}
