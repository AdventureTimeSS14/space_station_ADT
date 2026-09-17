// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent]
public sealed partial class RMCExtinguisherPowerComponent : Component
{
    [DataField]
    public int Power = 7;
}
