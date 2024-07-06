using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Language;

[RegisterComponent]
public sealed partial class UnknowLanguageComponent : Component
{
    [DataField("language")]
    public string LanguageToForgot = "GalacticCommon";
}
