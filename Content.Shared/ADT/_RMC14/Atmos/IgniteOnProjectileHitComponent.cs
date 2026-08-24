// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IgniteOnProjectileHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Intensity = 30;

    [DataField, AutoNetworkedField]
    public int Duration = 20;

    [DataField, AutoNetworkedField]
    public Color BurnColor = Color.FromHex("#EE6515");

    [DataField, AutoNetworkedField]
    public bool BurnsInVacuum;

    [DataField, AutoNetworkedField]
    public TimeSpan VacuumBurnout = TimeSpan.FromSeconds(1.5);
}
