using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Rituals;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTEmpathComponent : Component
{
    [DataField]
    public EntProtoId Action = "ADTActionEmpath";

    [DataField]
    public EntityUid? ActionEntity;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTEmpathMemoryComponent : Component
{
    [DataField]
    public EntityUid? LastTarget;

    [DataField]
    public TimeSpan LastAttack;
}

public sealed partial class ADTEmpathActionEvent : Content.Shared.Actions.EntityTargetActionEvent
{
}
