using Content.Shared.ADT.Cooking.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Cooking.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTPlateComponent : Component
{
    public const string ContainerId = "adt_plate";

    [DataField]
    public ProtoId<ADTPlateLayoutPrototype> Layout = "ADTPlateLarge";

    [DataField]
    public int MaxItems = 4;

    [ViewVariables]
    public Container Contents = default!;

    [ViewVariables]
    public string? OriginalName;

    [ViewVariables]
    public ProtoId<ADTCookingRecipePrototype>? Serving;
}
