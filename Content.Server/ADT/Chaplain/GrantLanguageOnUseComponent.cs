using Content.Shared.ADT.Language;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.ADT.Chaplain.Components;

[RegisterComponent]
public sealed partial class GrantLanguageOnUseComponent : Component
{
    [DataField("language", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<LanguagePrototype>))]
    public string Language = default!;
}