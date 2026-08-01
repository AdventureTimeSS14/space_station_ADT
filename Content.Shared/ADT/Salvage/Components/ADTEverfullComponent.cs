using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Salvage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ADTEverfullComponent : Component
{
    [DataField]
    public string Solution = "drink";

    [DataField, AutoNetworkedField]
    public List<ADTEverfullOption> Options = new();

    [DataField]
    public TimeSpan RefillCooldown = TimeSpan.FromSeconds(30);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextRefill;

    [DataField]
    public FixedPoint2 FillAmount = FixedPoint2.New(1);

    [DataField]
    public TimeSpan FillInterval = TimeSpan.FromSeconds(0.5);

    [DataField]
    public ProtoId<ReagentPrototype>? Filling;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextFillTick;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ADTEverfullOption
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;

    [DataField]
    public EntProtoId? Icon;
}

[Serializable, NetSerializable]
public enum ADTEverfullUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ADTEverfullSelectMessage(int index) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
}
