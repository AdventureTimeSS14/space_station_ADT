using Content.Server.Cargo.Systems;
using Content.Shared.Stacks;

namespace Content.Server.Cargo.Components;

[RegisterComponent]
[Access(typeof(CargoSystem))]
public sealed partial class CargoPalletConsoleComponent : Component
{ //ADT tweak start
    [DataField]
    public string? CurrencyProto = null;
}
//ADT tweak end