using Content.Shared.ADT.Language;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Language;

/// <summary>
/// Сообщение увидят ТОЛЬКО те, кто владеет языком. Полезно для языков наподобие локального коллективного разума.
/// </summary>
[DataDefinition]
public sealed partial class OnlyToCanUnderstand : ILanguageCondition
{
    public ProtoId<LanguagePrototype> Language { get; set; }

    [DataField]
    public bool RaiseOnListener { get; set; } = true;

    public bool Condition(EntityUid targetEntity, EntityUid? source, IEntityManager entMan)
    {
        var lang = entMan.System<LanguageSystem>();
        return lang.CanUnderstand(targetEntity, Language);
    }
}
