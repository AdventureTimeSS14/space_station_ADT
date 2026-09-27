using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.ADT.Xenobiology.Potions;

public sealed class SlimePotionConsentWindow : DefaultWindow
{
    public readonly Button DenyButton;
    public readonly Button AcceptButton;
    private readonly Label _prompt;

    public SlimePotionConsentWindow(string titleKey)
    {
        Title = Loc.GetString(titleKey);

        _prompt = new Label { Text = string.Empty };

        Contents.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Children =
            {
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Children =
                    {
                        _prompt,
                        new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            Align = AlignMode.Center,
                            Children =
                            {
                                (AcceptButton = new Button
                                {
                                    Text = Loc.GetString("xeno-potion-consent-accept"),
                                }),

                                (new Control()
                                {
                                    MinSize = new Vector2(20, 0)
                                }),

                                (DenyButton = new Button
                                {
                                    Text = Loc.GetString("xeno-potion-consent-deny"),
                                })
                            }
                        },
                    }
                },
            }
        });
    }

    public void SetPrompt(string text)
    {
        _prompt.Text = text;
    }
}