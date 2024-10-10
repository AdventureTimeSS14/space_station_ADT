using System.Numerics;
using Content.Client.Eui;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

public sealed partial class AdtAmnesiaEui : BaseEui
{
    private readonly AdtAmnesiaWindow _window;

    public AdtAmnesiaEui()
    {
        _window = new();
        _window.OkButton.OnPressed += _ =>
        {
            _window.Close();
        };
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
        base.Opened();
    }

    public override void Closed()
    {
        _window.Close();
    }
}

public sealed class AdtAmnesiaWindow : DefaultWindow
{
    public readonly Button OkButton;

    public AdtAmnesiaWindow()
    {
        Title = Loc.GetString("accept-adt_amnesia-window-title");

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
                            new Label()
                            {
                                Text = Loc.GetString("adt_amnesia-window-prompt-text-part")
                            },
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Horizontal,
                                Align = AlignMode.Center,
                                Children =
                                {
                                    (OkButton = new Button
                                    {
                                        Text = "OK",
                                    }),

                                    new Control()
                                    {
                                        MinSize = new Vector2(20, 0)
                                    },
                                }
                            },
                        }
                    },
                }
        });
    }
}
