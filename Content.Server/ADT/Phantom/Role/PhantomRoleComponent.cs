using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Roles;

/// <summary>
///     Added to mind entities to tag that they are a nuke operative.
/// </summary>
[RegisterComponent]
public sealed partial class PhantomRoleComponent : BaseMindRoleComponent
{

    [DataField("prototype")]
    public ProtoId<AntagPrototype>? AntagPrototype { get; set; }
}
