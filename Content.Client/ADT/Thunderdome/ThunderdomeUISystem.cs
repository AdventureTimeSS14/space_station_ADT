using Content.Shared.ADT.Thunderdome;
using Content.Shared.Popups;

namespace Content.Client.ADT.Thunderdome;

public sealed partial class ThunderdomeUISystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private ThunderdomeRevivalWindow? _revivalWindow;
    private ThunderdomeLeaderboardWindow? _leaderboardWindow;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ThunderdomeRevivalOfferEvent>(OnRevivalOffer);
        SubscribeNetworkEvent<ThunderdomeAnnouncementEvent>(OnAnnouncement);
        SubscribeNetworkEvent<ThunderdomeLeaderboardEvent>(OnLeaderboard);
    }

    private void OnAnnouncement(ThunderdomeAnnouncementEvent ev)
    {
        _popup.PopupCursor(ev.Message, PopupType.LargeCaution);
    }

    private void OnRevivalOffer(ThunderdomeRevivalOfferEvent ev)
    {
        _revivalWindow?.Close();
        _revivalWindow = new ThunderdomeRevivalWindow();
        _revivalWindow.OnAccepted += () =>
        {
            RaiseNetworkEvent(new ThunderdomeRevivalAcceptEvent());
        };
        _revivalWindow.OpenCentered();
    }

    private void OnLeaderboard(ThunderdomeLeaderboardEvent ev)
    {
        if (_leaderboardWindow == null || _leaderboardWindow.Disposed)
        {
            _leaderboardWindow = new ThunderdomeLeaderboardWindow();
            _leaderboardWindow.OnClose += () => _leaderboardWindow = null;
        }

        _leaderboardWindow.UpdateState(ev);
        _leaderboardWindow.OpenCentered();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _revivalWindow?.Close();
        _revivalWindow = null;

        _leaderboardWindow?.Close();
        _leaderboardWindow = null;
    }
}
