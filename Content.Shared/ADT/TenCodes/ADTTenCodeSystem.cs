using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.TenCodes;

public sealed class ADTTenCodeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public bool Knows(EntityUid? uid)
    {
        return uid != null && HasComp<ADTTenCodeKnowledgeComponent>(uid.Value);
    }

    public string Highlight(EntityUid speaker, string message)
    {
        if (string.IsNullOrEmpty(message) || !message.Contains(ADTTenCodes.Prefix))
            return message;

        if (!Knows(speaker))
            return message;

        return ADTTenCodes.CodeRegex.Replace(message, match =>
        {
            if (!_prototype.HasIndex<ADTTenCodePrototype>(match.Value))
                return match.Value;

            return $"[{ADTTenCodes.MarkupTag}=\"{match.Value}\"/]";
        });
    }

    public bool TryGetDescription(string code, [NotNullWhen(true)] out string? description)
    {
        description = null;

        if (!_prototype.TryIndex<ADTTenCodePrototype>(code, out var proto))
            return false;

        description = Loc.GetString(proto.Description);
        return true;
    }
}
