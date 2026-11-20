using Content.Shared.ADT.RoundIntro;

namespace Content.Client.GameTicking.Managers;

public sealed partial class ClientGameTicker
{
    private bool ADTRoundIntroJoinGameAttempt()
    {
        var ev = new ClientJoinGameAttemptEvent();
        RaiseLocalEvent(ref ev);
        return ev.Handled;
    }
}
