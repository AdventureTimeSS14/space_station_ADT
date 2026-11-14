//

using Content.Shared.Actions.Components;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RecallableItemComponent : Component
{
    [DataField(required: true)]
    public EntProtoId<ActionComponent> ActionId;

    [DataField]
    public EntityUid? Action;

    [DataField]
    public EntityUid? User;

    [DataField]
    public EntityWhitelist? UserWhitelist;

    [DataField]
    public EntityWhitelist? UserBlacklist;

    [DataField]
    public bool WhitelistCheckMind;
}
