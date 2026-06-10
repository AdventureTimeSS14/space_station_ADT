using Content.Shared.ADT.Grab;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.MartialArts;

[RegisterComponent]
public sealed partial class MartialArtBlockedComponent : Component
{
    [DataField]
    public MartialArtsForms Form;
}

public abstract partial class GrabStagesOverrideComponent : Component
{
    public readonly GrabStage StartingStage = GrabStage.Soft;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MartialArtsKnowledgeComponent : GrabStagesOverrideComponent
{
    [DataField, AutoNetworkedField]
    public MartialArtsForms MartialArtsForm = MartialArtsForms.CloseQuartersCombat;

    [DataField, AutoNetworkedField]
    public bool Blocked;

    [DataField, AutoNetworkedField]
    public float OriginalFistDamage;

    [DataField, AutoNetworkedField]
    public string OriginalFistDamageType = "Blunt";
}

public enum MartialArtsForms
{
    CorporateJudo,
    CloseQuartersCombat,
    SleepingCarp,
    Capoeira,
    KungFuDragon,
    Ninjutsu,
    HellRip,
}
