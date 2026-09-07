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

    private const string D20IconName = "ADTD20Icon";
    private const float D20IconSize = 16f;

    private const float FullscreenDiceSize = 384f;
    private const float FullscreenDiceHold = 0.8f;
    private const float FullscreenDiceFade = 0.6f;

    private bool _d20Enabled;
    private TimeSpan _d20CooldownEnd;
    private uint _d20ToggleFrame;
    private Texture? _d20IconTexture;

    public void HandleD20SelectorPressed(ChatSelectChannel channel)
    {
        if (channel != ChatSelectChannel.Emotes)
        {
            SetD20Enabled(false);
            return;
        }

        if (_input.IsKeyDown(Keyboard.Key.Shift))
            ToggleD20();
    }

    public bool TryHandleD20ShiftClick(ChannelSelectorItemButton button, BoundKeyFunction function)
    {
        if (button.Channel != ChatSelectChannel.Emotes)
            return false;

        if (!IsShiftLeftClick(function))
            return false;

        ToggleD20();
        SelectEmotesChannel(button);
        return true;
    }

    private void ToggleD20()
    {
        if (_d20ToggleFrame == _timing.CurFrame)
            return;

        _d20ToggleFrame = _timing.CurFrame;

        if (_d20Enabled)
        {
            SetD20Enabled(false);
            return;
        }

        if (_timing.RealTime < _d20CooldownEnd)
        {
            AddD20Line(GetD20CooldownMessage());
            return;
        }

        SetD20Enabled(true);
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

    public void UpdateD20Icon(Button button, ChatSelectChannel channel)
    {
        SetD20Icon(button, _d20Enabled && channel == ChatSelectChannel.Emotes);
    }

    private void SetD20Icon(Button button, bool enabled)
    {
        var icon = FindD20Icon(button);

        if (!enabled)
        {
            if (icon == null)
                return;

            icon.Orphan();
            button.Label.Margin = new Thickness();
            return;
        }

        if (icon != null)
            return;

        if (_d20IconTexture == null)
        {
            var sprite = GetSpriteSystem();
            if (sprite == null)
                return;

            _d20IconTexture = ADTD20RollControl.GetFace(sprite, SharedADTD20Emote.Faces);
        }

        button.Label.Margin = new Thickness(0f, 0f, D20IconSize + 2f, 0f);
        button.AddChild(new TextureRect
        {
            Name = D20IconName,
            Texture = _d20IconTexture,
            SetSize = new Vector2(D20IconSize, D20IconSize),
            Stretch = TextureRect.StretchMode.Scale,
            HorizontalAlignment = Control.HAlignment.Right,
            VerticalAlignment = Control.VAlignment.Center,
            MouseFilter = Control.MouseFilterMode.Ignore,
        });
    }

    private SpriteSystem? GetSpriteSystem()
    {
        if (_sprite != null)
            return _sprite;

        if (_ent.EntitySysManager.TryGetEntitySystem<SpriteSystem>(out var sprite))
            return sprite;

        return null;
    }

    private static TextureRect? FindD20Icon(Button button)
    {
        foreach (var child in button.Children)
        {
            if (child is TextureRect icon && icon.Name == D20IconName)
                return icon;
        }

        return null;
    }

    private bool TrySendD20Emote(ChatBox box, ChatSelectChannel channel, string text)
    {
        if (!_d20Enabled || channel != ChatSelectChannel.Emotes)
            return false;

        SetD20Enabled(false);

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

    private void SetD20Enabled(bool enabled)
    {
        if (_d20Enabled == enabled)
            return;

        _d20Enabled = enabled;
        RefreshD20Buttons();
    }

    private void RefreshD20Buttons()
    {
        foreach (var chat in _chats)
        {
            UpdateSelectedChannel(chat);
            RefreshD20PopupButtons(chat.ChatInput.ChannelSelector.Popup);
        }
    }

    private void RefreshD20PopupButtons(Control control)
    {
        foreach (var child in control.Children)
        {
            if (child is ChannelSelectorItemButton item)
            {
                UpdateD20Icon(item, item.Channel);
                continue;
            }

            RefreshD20PopupButtons(child);
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
