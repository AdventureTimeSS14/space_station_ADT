using System.Numerics;
using Content.Client.ADT.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Chat.Controls;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Shared.ADT.Chat;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController
{
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [UISystemDependency] private readonly SpriteSystem? _sprite = default;

    private enum EmoteMode : byte
    {
        Normal,
        D20,
        Do,
    }

    private const string ModeIconName = "ADTEmoteModeIcon";
    private const float ModeIconSize = 16f;

    private const float FullscreenDiceSize = 384f;
    private const float FullscreenDiceHold = 0.8f;
    private const float FullscreenDiceFade = 0.6f;

    private EmoteMode _emoteMode;
    private TimeSpan _d20CooldownEnd;
    private uint _modeToggleFrame;
    private Texture? _d20IconTexture;

    public void HandleEmoteModeSelectorPressed(ChatSelectChannel channel)
    {
        if (channel != ChatSelectChannel.Emotes)
        {
            SetEmoteMode(EmoteMode.Normal);
            return;
        }

        if (_input.IsKeyDown(Keyboard.Key.Shift))
            CycleEmoteMode();
    }

    public bool TryHandleEmoteModeShiftClick(ChannelSelectorItemButton button, BoundKeyFunction function)
    {
        if (button.Channel != ChatSelectChannel.Emotes)
            return false;

        if (!IsShiftLeftClick(function))
            return false;

        CycleEmoteMode();
        SelectEmotesChannel(button);
        return true;
    }

    private void CycleEmoteMode()
    {
        if (_modeToggleFrame == _timing.CurFrame)
            return;

        _modeToggleFrame = _timing.CurFrame;

        var next = _emoteMode switch
        {
            EmoteMode.Normal => EmoteMode.D20,
            EmoteMode.D20 => EmoteMode.Do,
            _ => EmoteMode.Normal,
        };

        if (next == EmoteMode.D20 && _timing.RealTime < _d20CooldownEnd)
        {
            AddD20Line(GetD20CooldownMessage());
            next = EmoteMode.Do;
        }

        SetEmoteMode(next);
    }

    private void SelectEmotesChannel(Control button)
    {
        foreach (var chat in _chats)
        {
            if (!IsDescendantOf(button, chat.ChatInput.ChannelSelector.Popup))
                continue;

            chat.SafelySelectChannel(ChatSelectChannel.Emotes);
            return;
        }
    }

    private static bool IsDescendantOf(Control control, Control parent)
    {
        for (var current = control.Parent; current != null; current = current.Parent)
        {
            if (current == parent)
                return true;
        }

        return false;
    }

    private bool IsShiftLeftClick(BoundKeyFunction function)
    {
        if (!_input.TryGetKeyBinding(function, out var binding))
            return false;

        if (binding.BaseKey != Keyboard.Key.MouseLeft)
            return false;

        return binding.Mod1 == Keyboard.Key.Shift
            || binding.Mod2 == Keyboard.Key.Shift
            || binding.Mod3 == Keyboard.Key.Shift;
    }

    public void UpdateEmoteModeIcon(Button button, ChatSelectChannel channel)
    {
        SetEmoteModeIcon(button, channel == ChatSelectChannel.Emotes ? _emoteMode : EmoteMode.Normal);
    }

    private void SetEmoteModeIcon(Button button, EmoteMode mode)
    {
        var current = FindModeIcon(button);

        if (mode == EmoteMode.Normal)
        {
            if (current == null)
                return;

            current.Orphan();
            button.Label.Margin = new Thickness();
            return;
        }

        if (current != null)
        {
            if (mode == EmoteMode.D20 && current is TextureRect)
                return;

            if (mode == EmoteMode.Do && current is Label)
                return;

            current.Orphan();
        }

        var icon = mode == EmoteMode.D20 ? CreateD20Icon() : CreateDoLabel();
        if (icon == null)
        {
            button.Label.Margin = new Thickness();
            return;
        }

        button.Label.Margin = new Thickness(0f, 0f, ModeIconSize + 2f, 0f);
        button.AddChild(icon);
    }

    private Control? CreateD20Icon()
    {
        if (_d20IconTexture == null)
        {
            var sprite = GetSpriteSystem();
            if (sprite == null)
                return null;

            _d20IconTexture = ADTD20RollControl.GetFace(sprite, SharedADTD20Emote.Faces);
        }

        return new TextureRect
        {
            Name = ModeIconName,
            Texture = _d20IconTexture,
            SetSize = new Vector2(ModeIconSize, ModeIconSize),
            Stretch = TextureRect.StretchMode.Scale,
            HorizontalAlignment = Control.HAlignment.Right,
            VerticalAlignment = Control.VAlignment.Center,
            MouseFilter = Control.MouseFilterMode.Ignore,
        };
    }

    private static Control CreateDoLabel()
    {
        return new Label
        {
            Name = ModeIconName,
            Text = Loc.GetString("adt-do-emote-mode-label"),
            FontColorOverride = Color.FromHex(SharedADTDoEmote.ColorHex),
            MinSize = new Vector2(ModeIconSize, 0f),
            HorizontalAlignment = Control.HAlignment.Right,
            VerticalAlignment = Control.VAlignment.Center,
            MouseFilter = Control.MouseFilterMode.Ignore,
        };
    }

    private SpriteSystem? GetSpriteSystem()
    {
        if (_sprite != null)
            return _sprite;

        if (_ent.EntitySysManager.TryGetEntitySystem<SpriteSystem>(out var sprite))
            return sprite;

        return null;
    }

    private static Control? FindModeIcon(Button button)
    {
        foreach (var child in button.Children)
        {
            if (child.Name == ModeIconName)
                return child;
        }

        return null;
    }

    private bool TrySendEmoteMode(ChatBox box, ChatSelectChannel channel, string text)
    {
        if (_emoteMode == EmoteMode.Normal || channel != ChatSelectChannel.Emotes)
            return false;

        var mode = _emoteMode;
        SetEmoteMode(EmoteMode.Normal);

        if (mode == EmoteMode.Do)
        {
            _consoleHost.ExecuteCommand($"do \"{CommandParsing.Escape(text)}\"");
            return true;
        }

        if (_timing.RealTime < _d20CooldownEnd)
        {
            box.AddLine(GetD20CooldownMessage(), Color.Orange);
            return false;
        }

        _d20CooldownEnd = _timing.RealTime + SharedADTD20Emote.Cooldown;
        _consoleHost.ExecuteCommand($"d20me \"{CommandParsing.Escape(text)}\"");
        return true;
    }

    private void FormatD20Emote(ChatMessage msg)
    {
        if (msg.Channel != ChatChannel.Emotes)
            return;

        if (!SharedADTD20Emote.TryGetRoll(msg.WrappedMessage, out var roll))
            return;

        var tier = Loc.GetString(SharedADTD20Emote.GetTierLocId(roll));

        msg.WrappedMessage = SharedADTD20Emote.ReplaceMarker(msg.WrappedMessage,
            " " + Loc.GetString("adt-d20-emote-result",
                ("color", SharedADTD20Emote.GetTierColor(roll)),
                ("tier", tier),
                ("roll", roll)));

        msg.Message = SharedADTD20Emote.ReplaceMarker(msg.Message,
            " " + Loc.GetString("adt-d20-emote-result-plain", ("tier", tier), ("roll", roll)));

        if (_player.LocalEntity != null && _ent.GetNetEntity(_player.LocalEntity.Value) == msg.SenderEntity)
            ShowFullscreenD20Roll(roll);
    }

    private void ShowFullscreenD20Roll(int roll)
    {
        var dice = new ADTD20RollControl(roll, FullscreenDiceSize, FullscreenDiceHold, FullscreenDiceFade)
        {
            HorizontalAlignment = Control.HAlignment.Center,
            VerticalAlignment = Control.VAlignment.Center,
        };

        dice.RollFinished += () => Timer.Spawn(0, dice.Orphan);

        LayoutContainer.SetAnchorPreset(dice, LayoutContainer.LayoutPreset.Wide);
        UIManager.PopupRoot.AddChild(dice);
    }

    private void SetEmoteMode(EmoteMode mode)
    {
        if (_emoteMode == mode)
            return;

        _emoteMode = mode;
        RefreshEmoteModeButtons();
    }

    private void RefreshEmoteModeButtons()
    {
        foreach (var chat in _chats)
        {
            UpdateSelectedChannel(chat);
            RefreshEmoteModePopupButtons(chat.ChatInput.ChannelSelector.Popup);
        }
    }

    private void RefreshEmoteModePopupButtons(Control control)
    {
        foreach (var child in control.Children)
        {
            if (child is ChannelSelectorItemButton item)
            {
                UpdateEmoteModeIcon(item, item.Channel);
                continue;
            }

            RefreshEmoteModePopupButtons(child);
        }
    }

    private string GetD20CooldownMessage()
    {
        var left = (int) Math.Ceiling((_d20CooldownEnd - _timing.RealTime).TotalSeconds);
        return Loc.GetString("adt-d20-emote-cooldown", ("seconds", Math.Max(left, 1)));
    }

    private void AddD20Line(string message)
    {
        ChatBox? target = null;

        foreach (var chat in _chats)
        {
            target ??= chat;

            if (!chat.Main)
                continue;

            target = chat;
            break;
        }

        target?.AddLine(message, Color.Orange);
    }
}
