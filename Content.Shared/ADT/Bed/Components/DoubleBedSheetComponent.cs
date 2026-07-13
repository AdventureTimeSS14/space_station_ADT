using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.ADT.Bed.Components;

[RegisterComponent, Access(typeof(DoubleBedSystem))]
public sealed partial class DoubleBedSheetComponent : Component
{
}
