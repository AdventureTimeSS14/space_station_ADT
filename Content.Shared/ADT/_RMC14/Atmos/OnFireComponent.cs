// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OnFireComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Intensity = 15;

    [DataField, AutoNetworkedField]
    public int Duration = 20;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? TileDamage;

    [DataField, AutoNetworkedField]
    public bool BurnsInVacuum;

    [DataField, AutoNetworkedField]
    public float VacuumDecay = 5f;
}
