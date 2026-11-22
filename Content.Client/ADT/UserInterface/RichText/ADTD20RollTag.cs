using System.Diagnostics.CodeAnalysis;
using Content.Client.ADT.UserInterface.Controls;
using Content.Shared.ADT.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.ADT.UserInterface.RichText;

public sealed class ADTD20RollTag : IMarkupTagHandler
{
    public const float DiceSize = 24f;
    public const float LineHeight = 16f;

    public string Name => "d20roll";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!node.Attributes.TryGetValue("result", out var parameter) || !parameter.TryGetLong(out var roll))
            return false;

        if (roll < 1 || roll > SharedADTD20Emote.Faces)
            return false;

        var dice = new ADTD20RollControl((int) roll, DiceSize);
        dice.FitToTextLine(LineHeight);

        control = dice;
        return true;
    }
}
