using System.Diagnostics.CodeAnalysis;
using Content.Shared.ADT.StationAi;
using Robust.Client.Network;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.ADT.StationAi;

public sealed class AiEyeLinkTag : IMarkupTagHandler
{
    [Dependency] private readonly IClientNetManager _net = default!;

    public string Name => "aieyelink";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!node.Value.TryGetString(out var text) ||
            !node.Attributes.TryGetValue("entity", out var entityParameter) ||
            !entityParameter.TryGetString(out var entityStr) ||
            !NetEntity.TryParse(entityStr, out var target))
        {
            return false;
        }

        var label = new Label
        {
            Text = text,
            MouseFilter = Control.MouseFilterMode.Stop,
            FontColorOverride = Color.LightBlue,
            DefaultCursorShape = Control.CursorShape.Hand,
        };

        label.OnMouseEntered += _ => label.FontColorOverride = Color.Blue;
        label.OnMouseExited += _ => label.FontColorOverride = Color.LightBlue;
        label.OnKeyBindDown += args => OnKeybindDown(args, target);

        if (node.Attributes.TryGetValue("title", out var titleArg))
            label.ToolTip = titleArg.StringValue;

        control = label;
        return true;
    }

    private void OnKeybindDown(GUIBoundKeyEventArgs args, NetEntity target)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _net.ClientSendMessage(new MsgAiEyeTeleport { Target = target });
    }
}
