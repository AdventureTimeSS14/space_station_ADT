using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.InconnuOS.UI;

public static class OsIcons
{
    public static void Draw(DrawingHandleScreen handle, UIBox2 box, OsAppIcon icon, Color accent)
    {
        var w = box.Width;
        var h = box.Height;
        var x = box.Left;
        var y = box.Top;

        var light = OsStyle.Mix(accent, Color.White, 0.35f);
        var dark = OsStyle.Mix(accent, Color.Black, 0.45f);

        switch (icon)
        {
            case OsAppIcon.Computer:
                OsDraw.RoundedRect(handle, new UIBox2(x, y + h * 0.1f, x + w, y + h * 0.7f), w * 0.08f, dark);
                handle.DrawRect(new UIBox2(x + w * 0.1f, y + h * 0.2f, x + w * 0.9f, y + h * 0.6f), light);
                handle.DrawRect(new UIBox2(x + w * 0.42f, y + h * 0.7f, x + w * 0.58f, y + h * 0.84f), dark);
                handle.DrawRect(new UIBox2(x + w * 0.24f, y + h * 0.84f, x + w * 0.76f, y + h * 0.92f), accent);
                break;

            case OsAppIcon.Folder:
                handle.DrawRect(new UIBox2(x + w * 0.06f, y + h * 0.2f, x + w * 0.46f, y + h * 0.32f), light);
                OsDraw.RoundedRect(handle, new UIBox2(x + w * 0.06f, y + h * 0.28f, x + w * 0.94f, y + h * 0.8f), w * 0.06f, accent);
                handle.DrawRect(new UIBox2(x + w * 0.06f, y + h * 0.36f, x + w * 0.94f, y + h * 0.42f), light);
                break;

            case OsAppIcon.Terminal:
                OsDraw.RoundedRect(handle, new UIBox2(x + w * 0.06f, y + h * 0.16f, x + w * 0.94f, y + h * 0.84f), w * 0.06f, Color.Black);
                handle.DrawRect(new UIBox2(x + w * 0.06f, y + h * 0.16f, x + w * 0.94f, y + h * 0.26f), dark);
                handle.DrawRect(new UIBox2(x + w * 0.18f, y + h * 0.44f, x + w * 0.34f, y + h * 0.5f), OsStyle.Good);
                handle.DrawRect(new UIBox2(x + w * 0.38f, y + h * 0.44f, x + w * 0.62f, y + h * 0.5f), OsStyle.Good);
                handle.DrawRect(new UIBox2(x + w * 0.18f, y + h * 0.58f, x + w * 0.5f, y + h * 0.64f), OsStyle.Good);
                break;

            case OsAppIcon.Circuit:
                OsDraw.RoundedRect(handle, new UIBox2(x + w * 0.22f, y + h * 0.22f, x + w * 0.78f, y + h * 0.78f), w * 0.06f, dark);
                handle.DrawRect(new UIBox2(x + w * 0.32f, y + h * 0.32f, x + w * 0.68f, y + h * 0.68f), accent);

                for (var i = 0; i < 3; i++)
                {
                    var offset = h * (0.3f + i * 0.18f);

                    handle.DrawRect(new UIBox2(x + w * 0.06f, offset, x + w * 0.22f, offset + h * 0.06f), light);
                    handle.DrawRect(new UIBox2(x + w * 0.78f, offset, x + w * 0.94f, offset + h * 0.06f), light);
                }

                break;

            case OsAppIcon.Notepad:
                OsDraw.RoundedRect(handle, new UIBox2(x + w * 0.16f, y + h * 0.1f, x + w * 0.84f, y + h * 0.9f), w * 0.05f, Color.FromHex("#e8e6dd"));

                for (var i = 0; i < 4; i++)
                {
                    var offset = y + h * (0.26f + i * 0.14f);

                    handle.DrawRect(new UIBox2(x + w * 0.26f, offset, x + w * 0.74f, offset + h * 0.05f), dark);
                }

                break;

            case OsAppIcon.Settings:
                handle.DrawCircle(new Vector2(x + w * 0.5f, y + h * 0.5f), w * 0.34f, accent);
                handle.DrawCircle(new Vector2(x + w * 0.5f, y + h * 0.5f), w * 0.15f, OsStyle.WindowBody);

                for (var i = 0; i < 4; i++)
                {
                    var angle = MathF.PI / 2f * i + MathF.PI / 4f;
                    var center = new Vector2(
                        x + w * 0.5f + MathF.Cos(angle) * w * 0.36f,
                        y + h * 0.5f + MathF.Sin(angle) * h * 0.36f);

                    handle.DrawCircle(center, w * 0.1f, accent);
                }

                break;

            case OsAppIcon.Tasks:
                handle.DrawRect(new UIBox2(x + w * 0.18f, y + h * 0.54f, x + w * 0.34f, y + h * 0.86f), light);
                handle.DrawRect(new UIBox2(x + w * 0.42f, y + h * 0.32f, x + w * 0.58f, y + h * 0.86f), accent);
                handle.DrawRect(new UIBox2(x + w * 0.66f, y + h * 0.44f, x + w * 0.82f, y + h * 0.86f), light);
                break;

            case OsAppIcon.Devices:
                OsDraw.RoundedRect(handle, new UIBox2(x + w * 0.1f, y + h * 0.26f, x + w * 0.9f, y + h * 0.74f), w * 0.08f, dark);

                for (var i = 0; i < 3; i++)
                {
                    handle.DrawCircle(new Vector2(x + w * (0.28f + i * 0.22f), y + h * 0.5f), w * 0.07f, accent);
                }

                break;

            case OsAppIcon.Disk:
                OsDraw.RoundedRect(handle, new UIBox2(x + w * 0.14f, y + h * 0.14f, x + w * 0.86f, y + h * 0.86f), w * 0.06f, dark);
                handle.DrawRect(new UIBox2(x + w * 0.32f, y + h * 0.14f, x + w * 0.68f, y + h * 0.42f), light);
                handle.DrawRect(new UIBox2(x + w * 0.26f, y + h * 0.56f, x + w * 0.74f, y + h * 0.86f), accent);
                break;

            default:
                OsDraw.RoundedRect(handle, new UIBox2(x + w * 0.16f, y + h * 0.16f, x + w * 0.84f, y + h * 0.84f), w * 0.1f, accent);
                handle.DrawRect(new UIBox2(x + w * 0.34f, y + h * 0.34f, x + w * 0.66f, y + h * 0.66f), OsStyle.WindowBody);
                break;
        }
    }
}

public sealed class OsIconControl : Control
{
    public OsAppIcon Icon;
    public Color Accent = Color.FromHex("#3f8fd0");

    public OsIconControl(OsAppIcon icon, float size)
    {
        Icon = icon;
        MinSize = new Vector2(size, size);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var side = MathF.Min(PixelWidth, PixelHeight);
        var left = (PixelWidth - side) / 2f;
        var top = (PixelHeight - side) / 2f;

        OsIcons.Draw(handle, new UIBox2(left, top, left + side, top + side), Icon, Accent);
    }
}
