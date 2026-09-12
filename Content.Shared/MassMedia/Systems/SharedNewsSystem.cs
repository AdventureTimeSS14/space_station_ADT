using Robust.Shared.Serialization;

namespace Content.Shared.MassMedia.Systems;

public abstract class SharedNewsSystem : EntitySystem
{
    public const int MaxTitleLength = 25;
    public const int MaxContentLength = 10000; /// ADT-Tweak
}

[Serializable, NetSerializable]
public struct NewsArticle
{
    [ViewVariables(VVAccess.ReadWrite)]
    public string Title;

    [ViewVariables(VVAccess.ReadWrite)]
    public string Content;

    [ViewVariables(VVAccess.ReadWrite)]
    public string? Author;

    [ViewVariables]
    public ICollection<(NetEntity, uint)>? AuthorStationRecordKeyIds;

    [ViewVariables]
    public TimeSpan ShareTime;

    // ADT-Tweak: комментарии читателей к статье
    [ViewVariables]
    public List<NewsComment>? Comments;
}

/// <summary>
///     A single reader comment on a news article. // ADT-Tweak
/// </summary>
[Serializable, NetSerializable]
public struct NewsComment
{
    [ViewVariables(VVAccess.ReadWrite)]
    public string? Author;

    [ViewVariables(VVAccess.ReadWrite)]
    public string Content;

    [ViewVariables]
    public TimeSpan ShareTime;
}

[ByRefEvent]
public record struct NewsArticlePublishedEvent(NewsArticle Article);

[ByRefEvent]
public record struct NewsArticleDeletedEvent;
