// SPDX-FileCopyrightText: 2025 LocalDuty
//
// SPDX-License-Identifier: MIT

using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.Lobby.UI;

/// <summary>
///     Логотип "LocalDuty" над кнопками лобби.
///     Local — #480607 (тёмно-красный), Duty — #53377A (фиолетовый).
///     Шрифт: LogoWIDEAWAKE.TTF
/// </summary>
public sealed class LobbyLogoDuty : Control
{
    public LobbyLogoDuty()
    {
        var resCache = IoCManager.Resolve<IResourceCache>();

        Font logoFont;
        try
        {
            logoFont = resCache.GetFont("/Fonts/Duty/LogoWIDEAWAKE.TTF", 42);
        }
        catch
        {
            logoFont = resCache.GetFont("/Fonts/NotoSans/NotoSans-Bold.ttf", 42);
        }

        var container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 0,
        };

        var localLabel = new Label
        {
            Text = "Local",
            FontColorOverride = Color.FromHex("#480607"),
            FontOverride = logoFont,
        };

        var dutyLabel = new Label
        {
            Text = "Duty",
            FontColorOverride = Color.FromHex("#53377A"),
            FontOverride = logoFont,
        };

        container.AddChild(localLabel);
        container.AddChild(dutyLabel);
        AddChild(container);
    }
}
