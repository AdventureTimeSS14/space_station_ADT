using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.Mobs;

[RegisterComponent]
public sealed partial class TryCatchBreathComponent : Component
{
    [DataField]
    public TimeSpan DoAfterTime = TimeSpan.FromSeconds(6);

    [DataField]
    public LocId TryPopup = "catch-breath-try";

    [DataField]
    public SoundSpecifier? TrySound;

    [DataField]
    public List<CatchBreathOutcome> Outcomes = new();
}

[DataDefinition]
public sealed partial class CatchBreathOutcome
{
    [DataField(required: true)]
    public float Weight;

    [DataField]
    public DamageSpecifier? Damage;

    [DataField(required: true)]
    public LocId Popup;

    [DataField]
    public SoundSpecifier? Sound;
}
