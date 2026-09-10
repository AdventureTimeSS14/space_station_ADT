using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared.ADT.Deafness;
using Content.Shared.Chat;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.ADT.Deafness;

public sealed class ADTDeafnessSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    private const int MinFragmentLength = 4;

    public void Deafen(EntityUid uid, float severity)
    {
        if (severity <= 0)
            return;

        var comp = EnsureComp<ADTDeafenedComponent>(uid);
        comp.Severity += severity;
        Dirty(uid, comp);
    }

    public bool IsFullyDeafened(EntityUid uid)
    {
        return TryComp<ADTDeafenedComponent>(uid, out var comp) && comp.Severity >= comp.TotalThreshold;
    }

    public bool IsDeafened(EntityUid uid)
    {
        return TryComp<ADTDeafenedComponent>(uid, out var comp) && comp.Severity > 0;
    }

    public bool TryGetTTSHearing(EntityUid uid, out bool muffled)
    {
        muffled = false;

        if (!TryComp<ADTDeafenedComponent>(uid, out var comp) || comp.Severity <= 0)
            return false;

        muffled = comp.Severity < comp.TotalThreshold;
        return true;
    }

    public bool TryInterceptRadio(EntityUid listener, ICommonSession session, string message, EntityUid source)
    {
        if (listener == source)
            return false;

        if (!TryGetDeafenedMessage(listener, message, out var perceived))
            return false;

        _chatManager.ChatMessageToOne(ChatChannel.Radio, perceived, perceived, source, false, session.Channel);
        return true;
    }

    public bool TryGetDeafenedMessage(EntityUid listener, string message, out string perceived)
    {
        perceived = string.Empty;

        if (!TryComp<ADTDeafenedComponent>(listener, out var comp) || comp.Severity <= 0)
            return false;

        if (comp.Severity >= comp.TotalThreshold)
        {
            perceived = Loc.GetString(comp.TotalMessage);
            return true;
        }

        perceived = PickFragment(message) is { } word
            ? Loc.GetString(comp.PartialMessage, ("word", word))
            : Loc.GetString(comp.TotalMessage);

        return true;
    }

    private string? PickFragment(string message)
    {
        var words = message
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => word.Trim('.', ',', '!', '?', ';', ':', '"', '\'', '(', ')'))
            .Where(word => word.Length >= MinFragmentLength)
            .ToList();

        if (words.Count == 0)
            return null;

        return _random.Pick(words);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ADTDeafenedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Severity -= comp.DecayPerSecond * frameTime;

            if (comp.Severity > 0)
            {
                Dirty(uid, comp);
                continue;
            }

            _popup.PopupEntity(Loc.GetString(comp.RecoveryMessage), uid, uid);
            RemComp<ADTDeafenedComponent>(uid);
        }
    }
}
