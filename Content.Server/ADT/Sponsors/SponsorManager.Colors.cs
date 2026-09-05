using System.Threading.Tasks;
using Content.Server.Players.RateLimiting;
using Content.Shared.ADT.Sponsors;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Enums;
using Robust.Shared.Network;

namespace Content.Server.ADT.Sponsors;

public sealed partial class SponsorManager
{
    [Dependency] private readonly PlayerRateLimitManager _rateLimit = default!;

    private const string ColorsRateLimitKey = "AdtSponsorColors";

    private readonly Dictionary<NetUserId, SponsorPersonalColors> _colors = new();
    private readonly HashSet<NetUserId> _colorSaves = new();

    public Color? GetOocColor(NetUserId userId)
    {
        var data = GetData(userId);

        if (data.AllowCustomOocColor && _colors.TryGetValue(userId, out var personal) && personal.Ooc != null)
            return personal.Ooc;

        return data.OocColor;
    }

    public Color? GetGhostColor(NetUserId userId)
    {
        if (!_colors.TryGetValue(userId, out var personal) || personal.Ghost == null)
            return null;

        if (!GetData(userId).IsGhostColorAllowed(personal.Ghost.Value))
            return null;

        return personal.Ghost;
    }

    private void RegisterColorRateLimit()
    {
        _rateLimit.Register(ColorsRateLimitKey,
            new RateLimitRegistration(
                SponsorCVars.ColorsRateLimitPeriod,
                SponsorCVars.ColorsRateLimitCount,
                null));
    }

    private async void OnSetColors(MsgSetSponsorColors message)
    {
        var userId = message.MsgChannel.UserId;

        try
        {
            var data = GetData(userId);

            if (!data.AllowCustomOocColor && !data.AllowCustomGhostColor && data.GhostColors.Count == 0)
                return;

            if (IsColorSpam(userId))
                return;

            var accepted = new SponsorPersonalColors();

            if (data.AllowCustomOocColor)
                accepted.Ooc = message.Colors.Ooc;

            if (message.Colors.Ghost != null && data.IsGhostColorAllowed(message.Colors.Ghost.Value))
                accepted.Ghost = message.Colors.Ghost;

            if (IsSameAsStored(userId, accepted))
            {
                SendState(userId);
                return;
            }

            _colors[userId] = accepted;

            if (!_colorSaves.Add(userId))
            {
                SendState(userId);
                return;
            }

            try
            {
                while (_colors.TryGetValue(userId, out var pending))
                {
                    await _db.SaveSponsorColorsAsync(userId.UserId, pending);

                    if (!_colors.TryGetValue(userId, out var current) || ReferenceEquals(current, pending))
                        break;
                }
            }
            finally
            {
                _colorSaves.Remove(userId);
            }

            if (_colors.ContainsKey(userId))
                SendState(userId);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось сохранить цвета для {userId}: {ex}");
        }
    }

    private bool IsColorSpam(NetUserId userId)
    {
        if (!_players.TryGetSessionById(userId, out var session) || session.Status == SessionStatus.Disconnected)
            return true;

        if (_rateLimit.CountAction(session, ColorsRateLimitKey) == RateLimitStatus.Allowed)
            return false;

        SendState(userId);
        return true;
    }

    private bool IsSameAsStored(NetUserId userId, SponsorPersonalColors accepted)
    {
        if (!_colors.TryGetValue(userId, out var stored))
            return false;

        return stored.Ooc == accepted.Ooc && stored.Ghost == accepted.Ghost;
    }

    private async Task LoadColors(NetUserId userId)
    {
        try
        {
            var colors = await _db.GetSponsorColorsAsync(userId.UserId);
            _colors[userId] = colors ?? new SponsorPersonalColors();
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось загрузить цвета для {userId}: {ex}");
            _colors[userId] = new SponsorPersonalColors();
        }
    }

    private (Color? Ooc, Color? Ghost) GetEffectiveColors(NetUserId userId)
    {
        return (GetOocColor(userId), GetGhostColor(userId));
    }
}
