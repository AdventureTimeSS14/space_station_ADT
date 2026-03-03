using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Body.Allergies.Prototypes;

[Prototype("allergicReaction")]
public sealed partial class AllergicReactionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("effects")]
    public EntityEffect[] Effects = default!;

    [DataField]
    public float StackThreshold;
}
