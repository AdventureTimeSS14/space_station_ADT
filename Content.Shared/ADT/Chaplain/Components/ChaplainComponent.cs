using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Content.Shared.Antag;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Shared.Alert;
using Robust.Shared.GameStates;  // ADT QWERTY's altar update

namespace Content.Shared.Bible.Components;


[RegisterComponent, NetworkedComponent]
public sealed partial class ChaplainComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Power = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PowerRegenCap = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Regenerated = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 BelieverCost = 3;

    [ViewVariables(VVAccess.ReadWrite)]
    public int Believers = 0;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int MaxBelievers = 2;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int MakeBelieverDuration = 7;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int TransferDuration = 2;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PowerTransfered = 1;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 HolyWaterCost = 2;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier HolyWaterSoundPath = new SoundPathSpecifier("/Audio/Effects/holy.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    public string WaterSolution = "Water";

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string WaterReplaceSolution = "Holywater";

    [ViewVariables(VVAccess.ReadWrite)]
    public string BloodSolution = "Blood";

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string BloodReplaceSolution = "Wine";

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? BelieverActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? TransferActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? HolyTouchActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PowerPerPray = 1;

    [ViewVariables]
    public float Accumulator = 0;

    [ViewVariables]
    public float RegenDelay = 30f;

    [DataField("statusIcon")]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BelieverFaction";

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AlertPrototype> Alert = "ADTChaplain";
}

