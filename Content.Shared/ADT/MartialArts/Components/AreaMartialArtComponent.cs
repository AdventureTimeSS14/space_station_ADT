using Content.Shared.ADT.Areas;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.MartialArts;

[RegisterComponent]
public sealed partial class AreaMartialArtComponent : Component
{
    [DataField(required: true)]
    public EntProtoId<AreaComponent> Area;

    [DataField(required: true)]
    public ProtoId<MartialArtPrototype> MartialArt;

    [DataField]
    public LocId? LearnMessage;

    public EntProtoId<AreaComponent>? LastArea;
}