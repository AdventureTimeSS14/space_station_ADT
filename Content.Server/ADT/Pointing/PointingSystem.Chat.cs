using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Pointing;
using Content.Shared.Chat;
using Robust.Server.Configuration;
using Robust.Shared.Player;

namespace Content.Server.Pointing.EntitySystems;

/// <summary>
/// partial-хук <see cref="PointingSystem.OnPointingChatMessage"/>.
/// </summary>
internal sealed partial class PointingSystem
{
    [Dependency] private readonly IChatManager _adtChatManager = default!;
    [Dependency] private readonly IServerNetConfigurationManager _adtNetConfig = default!;

    partial void OnPointingChatMessage(
        EntityUid source,
        ICommonSession sourceSession,
        IEnumerable<ICommonSession> viewers,
        EntityUid? iconTarget,
        string selfMessage,
        string viewerMessage,
        string? viewerPointedAtMessage)
    {
        if (!_config.GetCVar(ADTCCVars.PointingChatIconsEnabled))
            return;

        var viewerList = viewers
            .Append(sourceSession)
            .Distinct()
            .Where(v => _adtNetConfig.GetClientCVar(v.Channel, ADTCCVars.EnableChatPointingIcons))
            .ToList();

        if (viewerList.Count == 0)
            return;

        var iconNetId = iconTarget != null && Exists(iconTarget.Value)
            ? (int?) GetNetEntity(iconTarget.Value)
            : null;

        foreach (var viewer in viewerList)
        {
            var viewerEntity = viewer.AttachedEntity;
            if (viewerEntity is not { Valid: true })
                continue;

            var message = viewerEntity == source
                ? selfMessage
                : viewerEntity == iconTarget && viewerPointedAtMessage != null
                    ? viewerPointedAtMessage
                    : viewerMessage;

            var wrappedMessage = WrapPointingChatMessage(message, iconNetId);

            _adtChatManager.ChatMessageToOne(
                ChatChannel.Local,
                message,
                wrappedMessage,
                EntityUid.Invalid,
                false,
                viewer.Channel,
                Color.FromHex(PointingHelpers.PointingChatColorHex));
        }
    }

    private string WrapPointingChatMessage(string message, int? iconNetId)
    {
        var colored = Loc.GetString("chat-manager-pointing-wrap-message", ("message", message));

        if (iconNetId == null)
            return colored;

        var iconMarkup = PointingHelpers.BuildPointingChatMessage(string.Empty, iconNetId);
        return colored + iconMarkup;
    }
}
