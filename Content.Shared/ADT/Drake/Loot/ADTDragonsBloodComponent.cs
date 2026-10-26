using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Drake.Loot;

[RegisterComponent]
public sealed partial class ADTDragonsBloodComponent : Component
{
    [DataField(required: true)]
    public List<ADTDragonsBloodOutcome> Outcomes = new();

    [DataField]
    public SoundSpecifier? DrinkSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");
}

[DataDefinition]
public sealed partial class ADTDragonsBloodOutcome
{
    [DataField(required: true)]
    public LocId Message;

    [DataField]
    public EntProtoId? MindAction;

    [DataField]
    public LocId? KnownMessage;

    [DataField]
    public ComponentRegistry AddComponents = new();

    [DataField]
    public EntityEffect[] Effects = [];
}
