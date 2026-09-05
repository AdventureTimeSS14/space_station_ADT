using Content.Shared.ADT.Language;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.MindLink;

/// <summary>
/// Ментальная связь между парой сущностей: хранит партнёра и язык связи.
/// </summary>
[RegisterComponent]
public sealed partial class MindLinkComponent : Component
{
    [DataField]
    public EntityUid? Partner;

    /// <summary>
    /// Язык, выдаваемый участникам связи при её создании.
    /// </summary>
    [DataField]
    public ProtoId<LanguagePrototype> Language = "Universal";
}