using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.ComponentalActions.Components;

[RegisterComponent]
public sealed partial class HealActComponent : Component, IComponentalActionComponent
{
    [DataField]
    public EntProtoId Action { get; set; } = "CompActionHeal";

    [DataField]
    public EntityUid? ActionEntity { get; set; }

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/ADT/Blob/blobattack.ogg");

    [DataField]
    public float BruteHeal = -75f;

    [DataField]
    public float BurnHeal = -50f;

    [DataField]
    public float BloodRestore = 100f;

    [DataField]
    public float BleedReduce = -100f;
}
