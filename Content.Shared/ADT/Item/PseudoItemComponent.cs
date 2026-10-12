using Content.Shared.Cloning;
using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Shared.Item.PseudoItem;
/// <summary>
/// For entities that behave like an item under certain conditions,
/// but not under most conditions.
/// </summary>
[RegisterComponent]
public sealed partial class PseudoItemComponent : Component, ITransferredByCloning
{
    [DataField("size")]
    public ProtoId<ItemSizePrototype> Size = "Huge";

    public bool Active = false;

    [DataField]
    public EntityUid? SleepAction;
}