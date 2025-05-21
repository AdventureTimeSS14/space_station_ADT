using Content.Shared.ADT.Salvage.Systems;
using Content.Shared.Thief;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Salvage.Components;

/// <summary>
/// Thief toolbox except it uses a radial menu and has to be redeemed at the salvage vendor.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MiningVoucherSystem))]
[AutoGenerateComponentState]
public sealed partial class MiningVoucherComponent : Component
{
    /// <summary>
    /// Vendor must match this whitelist to be redeemed.
    /// </summary>
    [DataField]
    public EntityWhitelist? VendorWhitelist;

    /// <summary>
    /// The kits that can be selected.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<ThiefBackpackSetPrototype>> Kits = new();

    /// <summary>
    /// The index of the selected kit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? Selected;

    /// <summary>
    /// Sound to play when redeeming the voucher.
    /// </summary>
    [DataField]
    public SoundSpecifier? RedeemSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [DataField]
    public MiningVoucherTypeDrop TypeDrop = MiningVoucherTypeDrop.Default;

    [DataField]
    public MiningVoucherTypeDropPlace TypeDropPlace = MiningVoucherTypeDropPlace.Default;

}

[Serializable, NetSerializable]
public enum MiningVoucherUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum MiningVoucherTypeDrop : byte
{
    Default,
    Rocket
}

[Serializable, NetSerializable]
public enum MiningVoucherTypeDropPlace : byte
{
    Default,
    ThisPlace
}
