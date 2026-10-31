using Content.Shared.ADT.Surgery.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Surgery.Components;

[RegisterComponent]
public sealed partial class SurgeryToolComponent : Component
{
    [DataField]
    public float Speed = 1f;

    [DataField]
    public List<ProtoId<SurgeryToolQualityPrototype>> ExtraQualities = new();

    [DataField]
    public List<ProtoId<SurgeryToolQualityPrototype>>? ActiveQualities;

    [DataField]
    public SoundSpecifier? Sound;
}
