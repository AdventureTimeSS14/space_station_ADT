// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCFlamerBackpackComponent : Component
{
    [DataField, AutoNetworkedField]
    public string SolutionId = "tank";
}
