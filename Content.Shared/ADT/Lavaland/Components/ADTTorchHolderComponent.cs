using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Lavaland;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTTorchHolderComponent : Component
{
    [DataField]
    public string Slot = "torch";

    [DataField]
    public bool Ancient;

    [DataField]
    public DamageSpecifier BurnDamage = new();

    [DataField, AutoNetworkedField]
    public ADTTorchHolderStatus Status = ADTTorchHolderStatus.Empty;
}

[Serializable, NetSerializable]
public enum ADTTorchHolderStatus : byte
{
    Empty,
    Unlit,
    Lit,
    Burned,
}

[Serializable, NetSerializable]
public enum ADTTorchHolderVisuals : byte
{
    Status,
}
