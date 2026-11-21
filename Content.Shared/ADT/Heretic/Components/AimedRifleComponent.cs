//

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AimedRifleComponent : Component
{
    [DataField]
    public TimeSpan AimTimePerDistance = TimeSpan.FromMilliseconds(200);

    [DataField]
    public TimeSpan MaxAimTime = TimeSpan.FromSeconds(10);

    [DataField]
    public float MinDistance = 4f;

    [DataField]
    public float MaxDistance = 30f;

    [DataField, AutoNetworkedField]
    public EntityUid? AimingAt;

    [DataField, AutoNetworkedField]
    public EntityUid? AimingUser;

    [DataField]
    public EntityWhitelist? AimWhitelist;

    [DataField]
    public string AimUseDelayId = "aim";

    [DataField]
    public bool ShowMark = true;
}
