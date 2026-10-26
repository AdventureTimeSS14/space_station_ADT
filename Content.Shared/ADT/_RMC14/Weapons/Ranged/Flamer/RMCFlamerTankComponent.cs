// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCFlamerTankComponent : Component
{
    [DataField, AutoNetworkedField]
    public string SolutionId = "rmc_flamer_tank";

    [DataField, AutoNetworkedField]
    public int MaxIntensity = 40;

    [DataField, AutoNetworkedField]
    public int MaxDuration = 30;

    [DataField, AutoNetworkedField]
    public int MaxRange = 5;

    [DataField, AutoNetworkedField]
    public List<ProtoId<ReagentPrototype>>? ReagentWhitelist;
}
