using Content.Shared.ADT.Sponsors;
using Robust.Client.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client.ADT.Sponsors;

public sealed partial class SponsorManager : SharedSponsorManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    private SponsorData _data = SponsorData.Empty;

    public SponsorPersonalColors Colors { get; private set; } = new();

    public event Action? Updated;

    public override void Initialize()
    {
        base.Initialize();

        _netMgr.RegisterNetMessage<MsgSponsorState>(OnState);
        _netMgr.RegisterNetMessage<MsgSetSponsorColors>();

        InitializeLegacy();

        _netMgr.Disconnect += OnDisconnect;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        ShutdownLegacy();

        _netMgr.Disconnect -= OnDisconnect;
    }

    public SponsorData Data => _data;

    public override SponsorData GetData(ICommonSession? session)
    {
        if (session == null)
            return SponsorData.Empty;

        if (_players.LocalSession == null || session.UserId != _players.LocalSession.UserId)
            return SponsorData.Empty;

        return _data;
    }

    public void RequestColors(Color? ooc, Color? ghost)
    {
        var msg = new MsgSetSponsorColors
        {
            Colors = new SponsorPersonalColors
            {
                Ooc = ooc,
                Ghost = ghost,
            },
        };

        _netMgr.ClientSendMessage(msg);
    }

    private void OnState(MsgSponsorState message)
    {
        if (message.State == null)
        {
            Colors = new SponsorPersonalColors();
            SetData(SponsorData.Empty);
            return;
        }

        Colors = new SponsorPersonalColors
        {
            Ooc = message.State.SelectedOocColor,
            Ghost = message.State.SelectedGhostColor,
        };

        SetData(SponsorData.FromBenefits(
            message.State.Benefits,
            message.State.NextExpiry,
            message.State.Tiers));
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs e)
    {
        Colors = new SponsorPersonalColors();
        SetData(SponsorData.Empty);
    }

    private void SetData(SponsorData data)
    {
        _data = data;
        RaiseUpdated();
    }

    private void RaiseUpdated()
    {
        Updated?.Invoke();
    }
}
