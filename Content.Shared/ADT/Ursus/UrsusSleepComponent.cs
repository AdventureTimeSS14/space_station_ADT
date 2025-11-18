using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Ursus;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UrsusSleepComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? Action;
}

public sealed partial class UrsusSleepEvent : InstantActionEvent;
