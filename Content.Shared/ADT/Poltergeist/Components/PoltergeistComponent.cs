using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;

namespace Content.Shared.ADT.Poltergeist;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class PoltergeistComponent : Component
{

    #region Actions
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId MalfunctionAction = "ActionPoltergeistMalf";

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? MalfunctionActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId NoisyAction = "ActionPoltergeistNoisy";

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? NoisyActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId DieAction = "ActionPoltergeistRestInPeace";

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? DieActionEntity;
    #endregion

}

public sealed partial class PoltergeistMalfunctionActionEvent : EntityTargetActionEvent
{
}

public sealed partial class PoltergeistNoisyActionEvent : EntityTargetActionEvent
{
}

public sealed partial class PoltergeistRestInPeaceActionEvent : InstantActionEvent
{
}
