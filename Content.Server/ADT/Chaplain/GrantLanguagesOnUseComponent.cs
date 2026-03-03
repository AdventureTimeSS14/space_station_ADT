using Content.Shared.ADT.Language;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Chaplain.Components;

[RegisterComponent]
public sealed partial class GrantLanguagesOnUseComponent : Component
{
    [DataField("languages", required: true)]
    public List<ProtoId<LanguagePrototype>> Languages = new();
}
