using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.MartialArts;

[RegisterComponent]
public sealed partial class KravMagaActionComponent : Component
{
    [DataField]
    public KravMagaMoves Configuration;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public float StaminaDamage;

    [DataField]
    public int EffectTime;
}

[RegisterComponent]
public sealed partial class KravMagaComponent : GrabStagesOverrideComponent
{
    [DataField]
    public KravMagaMoves? SelectedMove;

    [DataField]
    public KravMagaActionComponent? SelectedMoveComp;

    public readonly List<EntProtoId> BaseKravMagaMoves = new()
    {
        "ActionLegSweep",
        "ActionNeckChop",
        "ActionLungPunch",
    };

    public readonly List<EntityUid> KravMagaMoveEntities = new();

    [DataField]
    public int BaseDamage = 5;

    [DataField]
    public int DownedDamageModifier = 2;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class KravMagaSilencedComponent : Component
{
    [DataField]
    public TimeSpan SilencedTime = TimeSpan.Zero;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class KravMagaBlockedBreathingComponent : Component
{
    [DataField]
    public TimeSpan BlockedTime = TimeSpan.Zero;
}

public enum KravMagaMoves
{
    LegSweep,
    NeckChop,
    LungPunch,
}
