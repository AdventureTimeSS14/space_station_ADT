using System.Threading.Tasks;
using Content.Shared.ADT.Sponsors;
using Robust.Shared.Network;

namespace Content.Server.ADT.Sponsors;

public sealed partial class SponsorManager
{
    private readonly Dictionary<NetUserId, SponsorPersonalColors> _colors = new();

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

    private async void OnSetColors(MsgSetSponsorColors message)
    {
        var userId = message.MsgChannel.UserId;

        try
        {
            var data = GetData(userId);
            var accepted = new SponsorPersonalColors();

            if (data.AllowCustomOocColor)
                accepted.Ooc = message.Colors.Ooc;

            if (message.Colors.Ghost != null && data.IsGhostColorAllowed(message.Colors.Ghost.Value))
                accepted.Ghost = message.Colors.Ghost;

            _colors[userId] = accepted;

            await _db.SaveSponsorColorsAsync(userId.UserId, accepted);

            if (_colors.ContainsKey(userId))
                SendState(userId);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось сохранить цвета для {userId}: {ex}");
        }
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
