using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Materials;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AutoMaterialInsertComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<TagPrototype> Tag;
}
