using Content.Shared.ADT.Spreader;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Spreader;

/// <summary>
/// Entity capable of becoming cloning and replicating itself to adjacent edges. See <see cref="SupermatterSpreaderSystem"/>
/// </summary>
[RegisterComponent, Access(typeof(SupermatterSpreaderSystem))]
public sealed partial class EdgeSupermatterSpreaderComponent : Component
{
    [DataField(required:true)]
    public ProtoId<EdgeSupermatterSpreaderPrototype> Id;
}
