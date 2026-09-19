using Content.Client.UserInterface.Controls;
using Content.Shared.Prototypes;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client.ADT.Heretic.BUI;

public sealed class MawedCrucibleMenu : FancyWindow
{
    public event Action<EntProtoId>? OnPotionSelected;

    public MawedCrucibleMenu(List<EntProtoId> potions, IPrototypeManager proto)
    {
        Title = Loc.GetString("mawed-crucible-ui-title");
        MinSize = new Vector2(300, 200);

        var vbox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(10),
        };

        var label = new Label
        {
            Text = Loc.GetString("mawed-crucible-ui-prompt"),
            HorizontalAlignment = Control.HAlignment.Center,
        };
        vbox.AddChild(label);

        foreach (var potion in potions)
        {
            if (potion == default)
                continue;

            var name = Loc.GetString($"ent-{potion}");
            if (string.IsNullOrEmpty(name))
                name = potion.Id;

            var button = new Button
            {
                Text = name,
                HorizontalAlignment = Control.HAlignment.Stretch,
                Margin = new Thickness(0, 2),
            };

            var capturedPotion = potion;
            button.OnPressed += _ =>
            {
                OnPotionSelected?.Invoke(capturedPotion);
                Close();
            };
            vbox.AddChild(button);
        }

        AddChild(vbox);
    }
}
