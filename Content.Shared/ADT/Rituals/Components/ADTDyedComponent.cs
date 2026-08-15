using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Rituals;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTDyedComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Dye;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTRitualTotemComponent : Component
{
}
