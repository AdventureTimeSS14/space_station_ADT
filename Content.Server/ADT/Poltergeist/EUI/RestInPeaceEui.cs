using Content.Server.EUI;
using Content.Shared.ADT.Phantom;
using Content.Shared.Eui;
using Content.Shared.ADT.Poltergeist;
using Content.Server.ADT.Poltergeist;

namespace Content.Server.ADT.Poltergeist;

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
