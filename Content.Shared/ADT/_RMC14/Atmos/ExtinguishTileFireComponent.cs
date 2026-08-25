// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
namespace Content.Shared._RMC14.Atmos;

[RegisterComponent]
public sealed partial class SprayExtinguishTileFireComponent : Component
{
    [ViewVariables]
    public bool Extinguished;

    [DataField]
    public TimeSpan ExtinguishAmount = TimeSpan.FromSeconds(6);
}
