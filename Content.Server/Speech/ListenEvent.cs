using Content.Shared.ADT.Language;

namespace Content.Server.Speech;

public sealed class ListenEvent : EntityEventArgs
{
    public readonly LanguagePrototype? Language; // ADT-Language
    public readonly string Message;
    public readonly EntityUid Source;

    public ListenEvent(string message, EntityUid source, LanguagePrototype? language = null) // ADT-Language
    {
        Language = language; // ADT-Language
        Message = message;
        Source = source;
    }
}

public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;

    public ListenAttemptEvent(EntityUid source)
    {
        Source = source;
    }
}
