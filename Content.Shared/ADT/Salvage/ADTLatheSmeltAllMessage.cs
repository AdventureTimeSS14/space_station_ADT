using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Salvage;

[Serializable, NetSerializable]
public sealed class ADTLatheSmeltAllMessage(ProtoId<LatheRecipePrototype> recipe) : BoundUserInterfaceMessage
{
    public readonly ProtoId<LatheRecipePrototype> Recipe = recipe;
}
