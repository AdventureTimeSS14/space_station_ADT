using System.Diagnostics.CodeAnalysis;
using Content.Server.ADT.Chat;
using Content.Shared.ADT.Language;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Language;

/// <summary>
/// Сообщение не увидят глухие персонажи
/// </summary>
[DataDefinition]
public sealed partial class CanHear : ILanguageCondition
{
    public ProtoId<LanguagePrototype> Language { get; set; }

    [DataField]
    public bool RaiseOnListener { get; set; } = true;

    public bool Condition(EntityUid targetEntity, EntityUid? source, IEntityManager entMan)
    {
        if (!source.HasValue)
            return false;

        var ev = new CanHearVoiceEvent(source.Value, false);
        entMan.EventBus.RaiseLocalEvent(targetEntity, ref ev);

        return !ev.Cancelled;
    }
}

