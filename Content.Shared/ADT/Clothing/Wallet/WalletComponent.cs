using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Clothing.Wallet;

[RegisterComponent, NetworkedComponent]
public sealed partial class WalletComponent : Component
{
    public int IdCardsInside;
}
