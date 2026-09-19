using System.Diagnostics.CodeAnalysis;
using System.Globalization; //ADT-Tweak
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.Heretic;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

public sealed class ButtonTag : IMarkupTagHandler
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    public string Name => "button";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!node.Attributes.TryGetValue("id", out var idParameter) ||
            !idParameter.TryGetString(out var id))
            return false;

        if (!node.Attributes.TryGetValue("label", out var labelParameter) ||
            !labelParameter.TryGetString(out var label))
            label = "";

        var ent = NetEntity.Invalid;
        var coords = NetCoordinates.Invalid;
        var timer = TimeSpan.Zero;

        if (node.Attributes.TryGetValue("uid", out var uidParameter) &&
            uidParameter.TryGetLong(out var uid))
            ent = new NetEntity((int) uid.Value);

        if (node.Attributes.TryGetValue("timer", out var timerParameter) &&
            timerParameter.TryGetLong(out var ms))
            timer = TimeSpan.FromMilliseconds(ms.Value);

        if (node.Attributes.TryGetValue("coords", out var coordsParameter) &&
            coordsParameter.TryGetString(out var coordsStr))
        {
            var split = coordsStr.Split(", ");
            //ADT-Tweak-Start
            if (split.Length >= 3 && int.TryParse(split[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var relativeUid) &&
                float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                coords = new NetCoordinates(new NetEntity(relativeUid), new Vector2(x, y));
            //ADT-Tweak-End
        }

        var button = new TimerButton(label, timer);
        button.OnPressed += _ =>
        {
            var ev = new ButtonTagPressedEvent(id, ent, coords);
            _net.SendSystemNetworkMessage(ev);
            button.Disabled = true;
        };
        button.HorizontalAlignment = Control.HAlignment.Left;
        button.VerticalAlignment = Control.VAlignment.Center;

        control = button;
        return true;
    }
}
