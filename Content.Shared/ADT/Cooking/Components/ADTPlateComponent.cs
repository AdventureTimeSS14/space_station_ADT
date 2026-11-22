using Content.Shared.ADT.Cooking.Prototypes;
using Content.Shared.Item;
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

    [DataField]
    public int MaxWeight = 10;

    [DataField]
    public ProtoId<ItemSizePrototype> FullSize = "Normal";

    [ViewVariables]
    public Container Contents = default!;

    [ViewVariables]
    public string? OriginalName;

    [ViewVariables]
    public ProtoId<ADTCookingRecipePrototype>? Serving;
}
