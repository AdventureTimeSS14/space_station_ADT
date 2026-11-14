//

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LionhunterRifleProjectileComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry ComponentsOnEmpower = new();

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public EntityUid? EmpowerTarget;

    [DataField, AutoNetworkedField]
    public string? ShooterPath;

    [DataField, AutoNetworkedField]
    public int ShooterPassiveLevel = 1;
}
