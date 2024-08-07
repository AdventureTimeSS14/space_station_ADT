using Content.Server.EUI;
using Content.Shared.Phantom;
using Content.Shared.Eui;
using Content.Shared.Poltergeist;
using Content.Server.Poltergeist;

namespace Content.Server.Poltergeist;

public sealed class RestInPeaceEui : BaseEui
{
    private readonly EntityUid _uid;
    private readonly PoltergeistSystem _poltergeist;

    public RestInPeaceEui(EntityUid uid, PoltergeistSystem polt)
    {
        _uid = uid;
        _poltergeist = polt;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not RestInPeaceChoiceMessage choice ||
            choice.Button == RestInPeaceButton.Deny)
        {
            Close();
            return;
        }

        _poltergeist.Rest(_uid);
        Close();
    }
}
