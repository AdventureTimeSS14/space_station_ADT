using Content.Shared.Alert;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Chaplain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ChaplainComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Power = 5;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PowerRegenCap = 5;

    [DataField]
    public TimeSpan RegenDelay = TimeSpan.FromSeconds(30);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRegen;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BelieverCost = 3;

    [DataField, AutoNetworkedField]
    public int Believers;

    [DataField, AutoNetworkedField]
    public int MaxBelievers = 2;

    [DataField]
    public TimeSpan MakeBelieverDuration = TimeSpan.FromSeconds(7);

    [DataField, AutoNetworkedField]
    public FixedPoint2 PowerTransfered = 1;

    [DataField, AutoNetworkedField]
    public FixedPoint2 HolyWaterCost = 2;

    [DataField]
    public SoundSpecifier HolyWaterSoundPath = new SoundPathSpecifier("/Audio/Effects/holy.ogg");

    [DataField]
    public ProtoId<ReagentPrototype> WaterSolution = "Water";

    [DataField]
    public ProtoId<ReagentPrototype> WaterReplaceSolution = "Holywater";

    [DataField]
    public ProtoId<ReagentPrototype> BloodSolution = "Blood";

    [DataField]
    public ProtoId<ReagentPrototype> BloodReplaceSolution = "Wine";

    [DataField]
    public FixedPoint2 RevenantDamage = 50;

    [DataField]
    public FixedPoint2 RevenantSelfDamage = 10;

    [DataField, AutoNetworkedField]
    public EntityUid? BelieverActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? TransferActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? HolyTouchActionEntity;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PowerPerPray = 1;

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "BelieverFaction";

    [DataField]
    public bool IconVisibleToGhost = true;

    [DataField]
    public ProtoId<AlertPrototype> Alert = "ADTChaplain";
}
