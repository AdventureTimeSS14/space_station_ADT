// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Shared.Chat;
using Robust.Shared.Player;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    public void SendMessageToOne(EntityUid target, string message, Color? colorOverride = null)
    {
        if (!TryComp<ActorComponent>(target, out var actor))
            return;

        var color = colorOverride ?? Color.White;
        _chatManager.ChatMessageToOne(ChatChannel.Server, message, message, target, false, actor.PlayerSession.Channel, color);
    }
}