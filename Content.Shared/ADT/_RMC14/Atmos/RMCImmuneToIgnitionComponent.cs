// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCImmuneToIgnitionComponent : Component
{
    [DataField, AutoNetworkedField]
    public int IntensityResistance = 80;

    [DataField, AutoNetworkedField]
    public bool ImmuneToDirectHits = true;
}
