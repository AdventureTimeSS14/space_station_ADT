using Content.Client.Eui;
using Content.Shared.ADT.Sponsors;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.ADT.Sponsors.UI;

[UsedImplicitly]
public sealed class SponsorPanelEui : BaseEui
{
    private readonly SponsorPanelWindow _window;

    public SponsorPanelEui()
    {
        _window = new SponsorPanelWindow();

        _window.OnClose += () => SendMessage(new CloseEuiMessage());

        _window.PlayerRequested += query =>
            SendMessage(new SponsorPanelEuiMsg.LookupPlayer { Query = query });

        _window.TierSaved += tier =>
            SendMessage(new SponsorPanelEuiMsg.SaveTier { Tier = tier });

        _window.TierDeleted += tierId =>
            SendMessage(new SponsorPanelEuiMsg.DeleteTier { TierId = tierId });

        _window.GrantCreateRequested += grant =>
            SendMessage(new SponsorPanelEuiMsg.SaveGrant { Grant = grant });

        _window.GrantRevokeRequested += grantId =>
            SendMessage(new SponsorPanelEuiMsg.RevokeGrant { GrantId = grantId });
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not SponsorPanelEuiState s)
            return;

        _window.SetPermission(s.HasPermission);
        _window.SetTiers(s.Tiers);
        _window.SetPlayer(s.PlayerName, s.Grants, s.Resolved);
        _window.SetStatus(s.Status);
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
        _window.Dispose();
    }
}
