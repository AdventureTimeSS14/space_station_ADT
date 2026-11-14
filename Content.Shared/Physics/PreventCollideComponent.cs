using Content.Shared.Whitelist; //ADT-Tweak
using Robust.Shared.GameStates;

namespace Content.Shared.Physics;

/// <summary>
/// Use this to allow a specific UID to prevent collides
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PreventCollideComponent : Component
{
    [AutoNetworkedField]
    public EntityUid Uid;

    //ADT-Tweak-Start
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
    //ADT-Tweak-End
}

