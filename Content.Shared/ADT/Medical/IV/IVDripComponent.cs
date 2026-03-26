using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Medical.IV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class IVDripComponent : Component
{

    [DataField(required: true), AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public List<FixedPoint2> TransferAmounts = new() { 1, 5, 10, 15 };

    [DataField, AutoNetworkedField]
    public FixedPoint2 CurrentTransferAmount = FixedPoint2.New(5);

    [DataField, AutoNetworkedField]
    public EntityUid? AttachedTo;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public string Slot = "pack";

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TransferDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan TransferAt;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public string AttachedState = "hooked";

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public string UnattachedState = "unhooked";

    /// <summary>
    ///     Percentages are from 0 to 100
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public List<(int Percentage, string State)> ReagentStates = new();

    [DataField, AutoNetworkedField]
    public Color FillColor;

    /// <summary>
    ///     From 0 to 100
    /// </summary>
    [DataField, AutoNetworkedField]
    public int FillPercentage;

    [DataField, AutoNetworkedField]
    public int Range = 2;

    [DataField]
    public DamageSpecifier? RipDamage;

    [DataField, AutoNetworkedField]
    public bool Injecting = true;
}

[Serializable, NetSerializable]
public enum IVDripVisualLayers
{
    Base,
    Reagent
}
