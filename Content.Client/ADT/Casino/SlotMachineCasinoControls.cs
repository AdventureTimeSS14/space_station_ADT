using Content.Client.Resources;
using Content.Shared.ADT.Casino;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Casino;

public static class SlotMachineUiColors
{
    public static readonly Color PanelBg = Color.FromHex("#14142e");
    public static readonly Color PanelBorder = Color.FromHex("#333333");
    public static readonly Color ReelBg = Color.FromHex("#0a0a1a");
    public static readonly Color ReelBorder = Color.FromHex("#444444");
    public static readonly Color Gold = Color.FromHex("#ffd700");
    public static readonly Color GoldDark = Color.FromHex("#1a0a00");
    public static readonly Color GoldBorder = Color.FromHex("#daa520");
    public static readonly Color GreenWin = Color.FromHex("#00ff88");
    public static readonly Color BlueText = Color.FromHex("#88ccff");
    public static readonly Color PinkBonus = Color.FromHex("#ff6eff");
    public static readonly Color OrangeHot = Color.FromHex("#ffaa33");
    public static readonly Color GreyLose = Color.FromHex("#888888");
    public static readonly Color GreyText = Color.FromHex("#bbbbbb");

    public static readonly Color LineRed = Color.FromHex("#ff4444");
    public static readonly Color LineBlue = Color.FromHex("#4488ff");
    public static readonly Color LineGreen = Color.FromHex("#00ff88");
    public static readonly Color LinePink = Color.FromHex("#ff6eff");
    public static readonly Color[] LineColors = { Gold, LineRed, LineBlue, LineGreen, LinePink };

    public static readonly Color[] ConfettiColors =
    {
        Color.FromHex("#ffd700"),
        Color.FromHex("#ff6eff"),
        Color.FromHex("#00ff88"),
        Color.FromHex("#ff4444"),
        Color.FromHex("#4488ff"),
    };
}

public sealed class SlotMachineFonts
{
    private const string Regular = "/Fonts/NotoSans/NotoSans-Regular.ttf";
    private const string Bold = "/Fonts/NotoSans/NotoSans-Bold.ttf";
    private const string Italic = "/Fonts/NotoSans/NotoSans-Italic.ttf";

    private readonly IResourceCache _res;
    private readonly Dictionary<(int Size, bool Bold), Font> _cache = new();


    public SlotMachineFonts(IResourceCache res)
    {
        _res = res;
    }

    public Font Get(int size, bool bold = false)
    {
        var key = (size, bold);
        if (_cache.TryGetValue(key, out var font))
            return font;

        font = _res.GetFont(bold ? Bold : Regular, size);
        _cache[key] = font;
        return font;
    }

    public Font ItalicFont(int size) => _res.GetFont(Italic, size);
}

public sealed class ReelsControl : Control
{
    private const int LabelW = 26;
    private const int PadX = 8;
    private const int PadY = 10;
    private const int ReelW = 60;
    private const int Gap = 6;
    private const int CellH = 50;
    private const int Rows = 3;
    private const float ScrollSpeed = 260f;
    private const int LandingWindows = 2;

    public string[][] Reels = Array.Empty<string[]>();
    public bool[] Stopped = Array.Empty<bool>();
    public bool Working;
    public int WinningLine;
    public Dictionary<string, Texture> Symbols = new();
    public float Time;

    private readonly float[] _scroll = new float[5];
    private readonly List<string>?[] _landStrip = new List<string>?[5];
    private readonly float[] _landFrom = new float[5];
    private readonly float[] _landLen = new float[5];
    private readonly float[] _landDur = new float[5];
    private readonly float[] _landT = new float[5];
    private readonly bool[] _wasStopped = new bool[5];
    private readonly bool[] _wasWorking = new bool[5];
    private float _rampT = 1f;

    public bool AllLanded
    {
        get
        {
            for (var i = 0; i < 5; i++)
            {
                if (_landStrip[i] != null && _landT[i] < 1f)
                    return false;
            }
            return true;
        }
    }

    public ReelsControl()
    {
        HorizontalExpand = true;
        MinSize = new Vector2(LabelW + 5 * (ReelW + Gap) - Gap + 2 * PadX, Rows * CellH + 2 * PadY);
        for (var i = 0; i < _wasStopped.Length; i++)
            _wasStopped[i] = true;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var dt = args.DeltaSeconds;
        for (var i = 0; i < 5; i++)
        {
            var stopped = !Working || (Stopped.Length > i && Stopped[i]);

            if (stopped && !_wasStopped[i] && Reels.Length > i && Reels[i]!.Length == Rows)
            {
                var step = (int)MathF.Floor(_scroll[i] / CellH);
                var land = new List<string>(3 + LandingWindows * 3 + 3);
                for (var j = step - 1; j < step + LandingWindows * 3; j++)
                    land.Add(SymbolAt(j));
                land.AddRange(Reels[i]);
                land.Add(SymbolAt(step + LandingWindows * 3));
                land.Add(SymbolAt(step + LandingWindows * 3 + 1));
                _landStrip[i] = land;
                _landFrom[i] = _scroll[i] - step * CellH;
                _landLen[i] = LandingWindows * 3 * CellH - _landFrom[i];

                _landDur[i] = 2f * _landLen[i] / ScrollSpeed;
                _landT[i] = 0f;
            }
            _wasStopped[i] = stopped;

            if (!stopped)
            {
                if (!_wasWorking[i])
                    _rampT = 0f;
                _rampT = MathF.Min(1f, _rampT + dt / 0.5f);
                _scroll[i] += ScrollSpeed * _rampT * _rampT * dt;
            }
            else if (_landStrip[i] != null && _landT[i] < 1f)
            {
                _landT[i] = MathF.Min(1f, _landT[i] + dt / _landDur[i]);
            }
            _wasWorking[i] = !stopped;
        }
    }

    private static string SymbolAt(int cell)
    {
        var ids = SlotMachineCasinoComponent.SymbolIds;
        return ids[((-cell) % ids.Length + ids.Length) % ids.Length];
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var size = PixelSize;

        handle.DrawRect(UIBox2.FromDimensions(0, 0, size.X, size.Y), SlotMachineUiColors.PanelBg);
        DrawBorder(handle, UIBox2.FromDimensions(0, 0, size.X, size.Y), SlotMachineUiColors.PanelBorder, 2);

        var reelsWidth = 5 * ReelW + 4 * Gap;
        var x0 = MathF.Max(LabelW + PadX, (size.X - reelsWidth) / 2f);

        for (var i = 0; i < 5; i++)
        {
            var x = x0 + i * (ReelW + Gap);
            var reelWindow = UIBox2.FromDimensions(x, PadY, ReelW, Rows * CellH);
            handle.DrawRect(reelWindow, SlotMachineUiColors.ReelBg);
            DrawBorder(handle, reelWindow, SlotMachineUiColors.ReelBorder, 2);

            var stopped = !Working || (Stopped.Length > i && Stopped[i]);
            var land = _landStrip[i];

            if (!stopped)
            {
                var pixelOffset = _scroll[i] % CellH;
                var step = (int)MathF.Floor(_scroll[i] / CellH);
                for (var r = -1; r <= Rows; r++)
                {
                    var sym = Symbols.GetValueOrDefault(SymbolAt(step + r));
                    if (sym == null)
                        continue;
                    var pos = new Vector2(x + (ReelW - 36) / 2f, PadY + r * CellH - pixelOffset + (CellH - 36) / 2f);
                    DrawClipped(handle, sym, pos, reelWindow);
                }
            }
            else if (land != null && _landT[i] < 1f)
            {
                var u = _landT[i];
                var l = _landFrom[i] + _landLen[i] * (2f * u - u * u);
                for (var k = 0; k < land.Count; k++)
                {
                    var sym = Symbols.GetValueOrDefault(land[k]);
                    if (sym == null)
                        continue;
                    var pos = new Vector2(x + (ReelW - 36) / 2f, PadY + (k - 1) * CellH - l + (CellH - 36) / 2f);
                    DrawClipped(handle, sym, pos, reelWindow);
                }
            }
            else if (Reels.Length > i && Reels[i]!.Length == Rows)
            {
                for (var r = 0; r < Rows; r++)
                {
                    var sym = Symbols.GetValueOrDefault(Reels[i][r]);
                    if (sym == null)
                        continue;
                    var pos = new Vector2(x + (ReelW - 36) / 2f, PadY + r * CellH + (CellH - 36) / 2f);
                    DrawClipped(handle, sym, pos, reelWindow);
                }

                handle.DrawRect(UIBox2.FromDimensions(x + 1, PadY + CellH, ReelW - 2, CellH), Color.FromHex("#ffd70026"));
                handle.DrawRect(UIBox2.FromDimensions(x + 1, PadY + CellH, ReelW - 2, 1), Color.FromHex("#ffd7004d"));
                handle.DrawRect(UIBox2.FromDimensions(x + 1, PadY + 2 * CellH - 1, ReelW - 2, 1), Color.FromHex("#ffd7004d"));
            }
        }

        var lineY = PadY + (int)(Rows * CellH / 2f);
        handle.DrawRect(UIBox2.FromDimensions(x0, lineY - 2, reelsWidth, 4), Color.FromHex("#ffd7002e"));
        handle.DrawRect(UIBox2.FromDimensions(x0, lineY - 1, reelsWidth, 2), Color.FromHex("#ffd700b3"));

        if (!Working && WinningLine > 0 && WinningLine <= SlotMachineCasinoComponent.Paylines.Length && Reels.Length == 5)
        {
            var color = SlotMachineUiColors.LineColors[WinningLine - 1];
            var pulse = 0.55f + 0.35f * MathF.Sin(Time * 5f);
            var payline = SlotMachineCasinoComponent.Paylines[WinningLine - 1];
            Vector2? prev = null;
            for (var col = 0; col < 5; col++)
            {
                var point = new Vector2(
                    x0 + col * (ReelW + Gap) + ReelW / 2f,
                    PadY + payline[col] * CellH + CellH / 2f);
                if (prev != null)
                    DrawThickLine(handle, prev.Value, point, color * new Color(1, 1, 1, pulse));
                prev = point;
            }
        }
    }

    private static void DrawClipped(DrawingHandleScreen handle, Texture tex, Vector2 pos, UIBox2 clip)
    {
        var dest = UIBox2.FromDimensions(pos.X, pos.Y, 36, 36);
        var left = MathF.Max(dest.Left, clip.Left);
        var top = MathF.Max(dest.Top, clip.Top);
        var right = MathF.Min(dest.Right, clip.Right);
        var bottom = MathF.Min(dest.Bottom, clip.Bottom);
        if (right <= left || bottom <= top)
            return;
        var inter = new UIBox2(left, top, right, bottom);
        var u0 = (inter.Left - dest.Left) / dest.Width * tex.Width;
        var v0 = (inter.Top - dest.Top) / dest.Height * tex.Height;
        var u1 = (inter.Right - dest.Left) / dest.Width * tex.Width;
        var v1 = (inter.Bottom - dest.Top) / dest.Height * tex.Height;
        handle.DrawTextureRectRegion(tex, inter, UIBox2.FromDimensions(u0, v0, u1 - u0, v1 - v0));
    }

    private static void DrawBorder(DrawingHandleScreen handle, UIBox2 box, Color color, int thickness)
    {
        handle.DrawRect(UIBox2.FromDimensions(box.Left, box.Top, box.Width, thickness), color);
        handle.DrawRect(UIBox2.FromDimensions(box.Left, box.Bottom - thickness, box.Width, thickness), color);
        handle.DrawRect(UIBox2.FromDimensions(box.Left, box.Top, thickness, box.Height), color);
        handle.DrawRect(UIBox2.FromDimensions(box.Right - thickness, box.Top, thickness, box.Height), color);
    }

    private static void DrawThickLine(DrawingHandleScreen handle, Vector2 from, Vector2 to, Color color)
    {
        var dir = to - from;
        var len = dir.Length();
        if (len < 0.1f)
            return;
        dir /= len;
        for (var d = 0f; d <= len; d += 2.5f)
        {
            var p = from + dir * d;
            handle.DrawRect(UIBox2.FromDimensions(p.X - 1.5f, p.Y - 1.5f, 3, 3), color);
        }
    }
}

public sealed class ConfettiControl : Control
{
    private float _time;

    public void Restart()
    {
        _time = 0;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _time += args.DeltaSeconds;
        if (_time > 2.6f)
            Visible = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        for (var i = 0; i < 30; i++)
        {
            var delay = (i * 0.13f) % 2f;
            var duration = 1.5f + i % 3 * 0.5f;
            var progress = (_time - delay) / duration;
            if (progress < 0f || progress > 1f)
                continue;
            var x = (i * 37) % 100 / 100f * PixelSize.X;
            var y = -10 + progress * (PixelSize.Y + 20);
            handle.DrawRect(
                UIBox2.FromDimensions(x, y, 8, 8),
                SlotMachineUiColors.ConfettiColors[i % SlotMachineUiColors.ConfettiColors.Length] * new Color(1, 1, 1, 1f - progress));
        }
    }
}

public sealed class IntroControl : Control
{
    private readonly Texture? _machine;

    public Label? ClickLabel;
    public float Time;

    public IntroControl(Texture? machine)
    {
        _machine = machine;
        MouseFilter = MouseFilterMode.Stop;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var size = PixelSize;

        // vertical gradient: #0a0a1a - #1a1a3e - #0a0a1a
        var bandH = size.Y / 2f;
        handle.DrawRect(UIBox2.FromDimensions(0, 0, size.X, bandH), Color.FromHex("#0a0a1a"));
        handle.DrawRect(UIBox2.FromDimensions(0, bandH, size.X, bandH), Color.FromHex("#1a1a3e"));
        var steps = 24;
        for (var i = 0; i < steps; i++)
        {
            var t = i / (float)steps;
            var c = Color.InterpolateBetween(Color.FromHex("#0a0a1a"), Color.FromHex("#1a1a3e"), t);
            handle.DrawRect(UIBox2.FromDimensions(0, size.Y / 2f - bandH / 2f + t * bandH, size.X, bandH / steps + 1), c);
        }

        if (_machine == null)
            return;

        var scale = 3;
        var mw = 32 * scale;
        var mh = 48 * scale;
        var mx = size.X / 2f - mw / 2f;
        var my = size.Y * 0.16f;
        handle.DrawTextureRectRegion(_machine, UIBox2.FromDimensions(mx, my, mw, mh));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (ClickLabel != null)
        {
            var pulse = 0.5f + 0.5f * MathF.Sin(Time * 4f);
            ClickLabel.ModulateSelfOverride = Color.White * new Color(1, 1, 1, 0.45f + 0.55f * pulse);
        }
    }
}

public sealed class ResultOverlayControl : Control
{
    private const float HoldTime = 2.0f;
    private const float FadeTime = 0.3f;

    private readonly Label _label = new();
    private float _time;
    private float _fadeAlpha = 1f;

    public ResultOverlayControl()
    {
        Visible = false;
        MouseFilter = MouseFilterMode.Ignore;
        HorizontalExpand = true;
        VerticalExpand = true;

        _label.Align = Label.AlignMode.Center;
        _label.VAlign = Label.VAlignMode.Center;
        _label.HorizontalExpand = true;
        _label.VerticalExpand = true;
        AddChild(_label);
    }

    public void Show(string text, Color color, Font font)
    {
        _label.Text = text;
        _label.FontColorOverride = color;
        _label.FontOverride = font;
        _time = 0f;
        _fadeAlpha = 1f;
        Visible = true;
    }

    public void Hide() => Visible = false;

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (!Visible)
            return;

        _time += args.DeltaSeconds;
        _fadeAlpha = _time <= HoldTime
            ? 1f
            : MathF.Max(0f, 1f - (_time - HoldTime) / FadeTime);

        var pulse = 0.85f + 0.15f * MathF.Sin(_time * 6f);
        _label.ModulateSelfOverride = Color.White * new Color(1, 1, 1, pulse * _fadeAlpha);

        if (_time >= HoldTime + FadeTime)
            Visible = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        handle.DrawRect(
            UIBox2.FromDimensions(0, 0, PixelSize.X, PixelSize.Y),
            Color.FromHex("#05050f") * new Color(1, 1, 1, 0.78f * _fadeAlpha));
    }
}
