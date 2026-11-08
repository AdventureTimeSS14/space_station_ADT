using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

public static class OsBootTimeline
{
    public const float PostEnd = 3.0f;
    public const float LogoStart = 3.4f;
    public const float LogoEnd = 6.8f;
    public const float WelcomeEnd = 8.2f;
    public const float Total = 8.7f;
}

public sealed class OsBootScreen : Control
{
    private const float PostLineDelay = 0.32f;
    private const int MemoryKilobytes = 16384;

    private readonly IGameTiming _timing;

    private readonly Font _mono;
    private readonly Font _monoBold;
    private readonly Font _brand;
    private readonly Font _text;
    private readonly Font _welcome;

    private ADTOsBuiState? _state;

    public OsBootScreen(IResourceCache cache, IGameTiming timing)
    {
        _timing = timing;

        MouseFilter = MouseFilterMode.Stop;

        _mono = OsStyle.CreateMonoFont(cache, 11);
        _monoBold = OsStyle.CreateMonoFont(cache, 13);
        _brand = OsStyle.CreateFont(cache, 26);
        _text = OsStyle.CreateFont(cache, 12);
        _welcome = OsStyle.CreateFont(cache, 22);
    }

    public void SetState(ADTOsBuiState state)
    {
        _state = state;
    }

    private float Elapsed
    {
        get
        {
            if (_state == null)
                return 0f;

            return (float) Math.Max(0, (_timing.CurTime - _state.BootedAt).TotalSeconds);
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_state == null)
            return;

        var elapsed = Elapsed;
        var box = new UIBox2(0f, 0f, PixelWidth, PixelHeight);

        if (elapsed < OsBootTimeline.PostEnd)
        {
            handle.DrawRect(box, Color.Black);
            DrawPost(handle, elapsed);
            return;
        }

        if (elapsed < OsBootTimeline.LogoStart)
        {
            handle.DrawRect(box, Color.Black);
            return;
        }

        if (elapsed < OsBootTimeline.LogoEnd)
        {
            handle.DrawRect(box, Color.Black);
            DrawLogoScreen(handle, elapsed - OsBootTimeline.LogoStart);
            return;
        }

        DrawWelcome(handle, elapsed - OsBootTimeline.LogoEnd);
    }

    private void DrawPost(DrawingHandleScreen handle, float time)
    {
        var state = _state!;
        var scale = UIScale;
        var left = 18f * scale;
        var line = 17f * scale;
        var top = 16f * scale;

        DrawBiosLogo(handle, new Vector2(PixelWidth - 70f * scale, 18f * scale), 11f * scale, state.Settings.Accent);

        handle.DrawString(_monoBold, new Vector2(left, top), Loc.GetString("os-post-title", ("publisher", OsBrand.Publisher)), 1f, Color.White);
        handle.DrawString(_mono, new Vector2(left, top + line), Loc.GetString("os-post-copyright", ("publisher", OsBrand.Publisher)), 1f, OsStyle.TextDim);

        var lines = BuildPostLines(state, time);
        var y = top + line * 3f;
        var shown = Math.Min(lines.Count, (int) (time / PostLineDelay));

        for (var i = 0; i < shown; i++)
        {
            handle.DrawString(_mono, new Vector2(left, y), lines[i], 1f, OsStyle.Text);
            y += line;
        }

        if (MathF.Sin(time * 14f) > 0f)
            handle.DrawString(_mono, new Vector2(left, y), "_", 1f, OsStyle.Text);

        var hint = Loc.GetString("os-post-hint");
        handle.DrawString(_mono, new Vector2(left, PixelHeight - line - 10f * scale), hint, 1f, OsStyle.TextDim);
    }

    private static List<string> BuildPostLines(ADTOsBuiState state, float time)
    {
        var memoryProgress = Math.Clamp((time - PostLineDelay) / 1.1f, 0f, 1f);
        var memory = (int) (MemoryKilobytes * memoryProgress) / 64 * 64;

        var lines = new List<string>
        {
            Loc.GetString("os-post-cpu"),
            memoryProgress < 1f
                ? Loc.GetString("os-post-memory", ("amount", memory))
                : Loc.GetString("os-post-memory-ok", ("amount", MemoryKilobytes)),
            string.Empty,
            Loc.GetString("os-post-drives"),
        };

        foreach (var drive in state.Drives)
        {
            lines.Add(Loc.GetString("os-post-drive",
                ("letter", drive.Letter),
                ("label", drive.Label),
                ("size", OsFormat.Size(drive.Capacity))));
        }

        lines.Add(Loc.GetString("os-post-keyboard"));
        lines.Add(string.Empty);
        lines.Add(Loc.GetString("os-post-booting", ("os", OsBrand.Name)));

        return lines;
    }

    private static void DrawBiosLogo(DrawingHandleScreen handle, Vector2 origin, float tile, Color accent)
    {
        var light = OsStyle.Mix(accent, Color.White, 0.4f);
        var gap = tile * 0.2f;

        handle.DrawRect(new UIBox2(origin.X, origin.Y, origin.X + tile, origin.Y + tile), accent);
        handle.DrawRect(new UIBox2(origin.X + tile + gap, origin.Y, origin.X + tile * 2f + gap, origin.Y + tile), light);
        handle.DrawRect(new UIBox2(origin.X, origin.Y + tile + gap, origin.X + tile, origin.Y + tile * 2f + gap), light);
        handle.DrawRect(new UIBox2(origin.X + tile + gap, origin.Y + tile + gap, origin.X + tile * 2f + gap, origin.Y + tile * 2f + gap), accent);
    }

    private void DrawLogoScreen(DrawingHandleScreen handle, float time)
    {
        var state = _state!;
        var scale = UIScale;
        var fade = Math.Clamp(time / 0.5f, 0f, 1f);
        var center = new Vector2(PixelWidth / 2f, PixelHeight * 0.4f);

        var accent = state.Settings.Accent.WithAlpha(fade);
        var light = OsStyle.Mix(state.Settings.Accent, Color.White, 0.4f).WithAlpha(fade);

        var tile = 26f * scale;
        var gap = tile * 0.12f;

        handle.DrawRect(new UIBox2(center.X - tile - gap, center.Y - tile - gap, center.X - gap, center.Y - gap), accent);
        handle.DrawRect(new UIBox2(center.X + gap, center.Y - tile - gap, center.X + tile + gap, center.Y - gap), light);
        handle.DrawRect(new UIBox2(center.X - tile - gap, center.Y + gap, center.X - gap, center.Y + tile + gap), light);
        handle.DrawRect(new UIBox2(center.X + gap, center.Y + gap, center.X + tile + gap, center.Y + tile + gap), accent);

        var name = OsBrand.Name;
        var nameSize = handle.GetDimensions(_brand, name, 1f);

        handle.DrawString(_brand,
            new Vector2(center.X - nameSize.X / 2f, center.Y + tile + 20f * scale),
            name, 1f, Color.White.WithAlpha(fade));

        if (time < 0.6f)
            return;

        OsDraw.Spinner(handle, new Vector2(center.X, PixelHeight * 0.76f), 16f * scale, 2.2f * scale, time, Color.White);
    }

    private void DrawWelcome(DrawingHandleScreen handle, float time)
    {
        var state = _state!;
        var scale = UIScale;
        var length = OsBootTimeline.WelcomeEnd - OsBootTimeline.LogoEnd;
        var fadeOut = 1f - Math.Clamp((time - length) / (OsBootTimeline.Total - OsBootTimeline.WelcomeEnd), 0f, 1f);
        var fadeIn = Math.Clamp(time / 0.3f, 0f, 1f);
        var alpha = fadeIn * fadeOut;

        var background = OsStyle.Mix(Color.Black, state.Settings.Accent, 0.45f);

        handle.DrawRect(new UIBox2(0f, 0f, PixelWidth, PixelHeight), background.WithAlpha(fadeOut));

        var center = new Vector2(PixelWidth / 2f, PixelHeight * 0.4f);
        var avatar = 44f * scale;

        handle.DrawCircle(center, avatar, OsStyle.Mix(background, Color.White, 0.25f).WithAlpha(alpha));
        handle.DrawCircle(center - new Vector2(0f, avatar * 0.25f), avatar * 0.32f, Color.White.WithAlpha(alpha * 0.85f));
        handle.DrawCircle(center + new Vector2(0f, avatar * 0.62f), avatar * 0.55f, Color.White.WithAlpha(alpha * 0.85f));

        var name = state.UserName;
        var nameSize = handle.GetDimensions(_welcome, name, 1f);

        handle.DrawString(_welcome,
            new Vector2(center.X - nameSize.X / 2f, center.Y + avatar + 18f * scale),
            name, 1f, Color.White.WithAlpha(alpha));

        var greeting = Loc.GetString("os-boot-welcome");
        var greetingSize = handle.GetDimensions(_text, greeting, 1f);
        var greetingY = center.Y + avatar + 30f * scale + nameSize.Y;

        OsDraw.Spinner(handle,
            new Vector2(center.X - greetingSize.X / 2f - 16f * scale, greetingY + greetingSize.Y / 2f),
            7f * scale, 1.3f * scale, time, Color.White.WithAlpha(alpha));

        handle.DrawString(_text, new Vector2(center.X - greetingSize.X / 2f, greetingY), greeting, 1f, Color.White.WithAlpha(alpha));
    }
}
