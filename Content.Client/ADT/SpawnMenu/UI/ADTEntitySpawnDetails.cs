using System.Linq;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.SpawnMenu.UI;

public sealed class ADTEntitySpawnDetails : BoxContainer
{
    [Dependency] private IClipboardManager _clipboard = default!;

    private static readonly Color CaptionColor = Color.FromHex("#A0A0A0");

    private readonly BoxContainer _content;
    private readonly Label _empty;
    private readonly ADTSpawnPreviewView _preview;
    private readonly Label _name;
    private readonly Label _id;
    private readonly ADTFlowContainer _tags;
    private readonly Label _variantsCaption;
    private readonly BoxContainer _variants;
    private readonly RichTextLabel _info;

    private readonly Button _favorite;
    private EntityPrototype? _shown;

    public Func<string, bool>? IsFavorite;

    public event Action<EntityPrototype>? OnVariantSelected;
    public event Action<string, string>? OnTagPressed;
    public event Action<EntityPrototype>? OnFavoriteToggled;

    public ADTEntitySpawnDetails()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 4;

        _empty = new Label
        {
            Text = Loc.GetString("adt-spawn-menu-details-empty"),
            StyleClasses = { "LabelSubText" },
            Align = Label.AlignMode.Center,
            Margin = new Thickness(0, 8),
        };
        AddChild(_empty);

        _preview = new ADTSpawnPreviewView
        {
            SetSize = new Vector2(96, 96),
            HorizontalAlignment = HAlignment.Center,
            Stretch = SpriteView.StretchMode.Fill,
        };

        _name = new Label
        {
            HorizontalAlignment = HAlignment.Center,
            StyleClasses = { "LabelKeyText" },
        };

        _id = new Label
        {
            HorizontalExpand = true,
            ClipText = true,
            StyleClasses = { "LabelSubText" },
            VerticalAlignment = VAlignment.Center,
        };

        var copy = new Button
        {
            Text = Loc.GetString("adt-spawn-menu-details-copy-id"),
            StyleClasses = { "ButtonSquare" },
        };
        copy.OnPressed += _ => CopyId();

        _favorite = new Button
        {
            ToggleMode = true,
            HorizontalExpand = true,
        };
        _favorite.OnPressed += _ =>
        {
            if (_shown != null)
                OnFavoriteToggled?.Invoke(_shown);
        };

        _tags = new ADTFlowContainer();

        _variantsCaption = new Label
        {
            Text = Loc.GetString("adt-spawn-menu-details-variants"),
            FontColorOverride = CaptionColor,
        };

        _variants = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };

        _info = new RichTextLabel
        {
            HorizontalExpand = true,
        };

        _content = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Children =
            {
                _preview,
                _name,
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    SeparationOverride = 4,
                    Children =
                    {
                        _id,
                        copy,
                    },
                },
                _favorite,
                _tags,
                _variantsCaption,
                _variants,
                _info,
            },
        };
        AddChild(_content);

        SetItem(null, null);
    }

    public void SetItem(ADTSpawnListItem? item, EntityPrototype? selected)
    {
        if (item == null)
        {
            _shown = null;
            _content.Visible = false;
            _empty.Visible = true;
            return;
        }

        _content.Visible = true;
        _empty.Visible = false;

        var entry = item.Representative;
        foreach (var variant in item.Variants)
        {
            if (variant.Prototype == selected)
                entry = variant;
        }

        var prototype = entry.Prototype;
        if (_shown != prototype)
            _preview.SetPrototype(prototype.ID);

        _shown = prototype;
        _name.Text = string.IsNullOrEmpty(prototype.Name) ? prototype.ID : prototype.Name;
        _id.Text = prototype.ID;
        _id.ToolTip = prototype.ID;

        var favorite = IsFavorite?.Invoke(prototype.ID) ?? false;
        _favorite.Pressed = favorite;
        _favorite.Text = Loc.GetString(favorite ? "adt-spawn-menu-favorite-remove" : "adt-spawn-menu-favorite-add");

        _tags.RemoveAllChildren();
        foreach (var (raw, normalized) in entry.SuffixTags)
        {
            var chip = new ADTSpawnTagChip(normalized, raw);
            chip.OnPressed += _ => OnTagPressed?.Invoke(raw, normalized);
            _tags.AddChild(chip);
        }

        _variants.RemoveAllChildren();
        _variantsCaption.Visible = item.Variants.Count > 1;
        if (item.Variants.Count > 1)
        {
            foreach (var variant in item.Variants)
            {
                var suffix = variant.Prototype.EditorSuffix;
                var button = new Button
                {
                    Text = string.IsNullOrWhiteSpace(suffix) ? Loc.GetString("adt-spawn-menu-details-no-suffix") : suffix,
                    ToolTip = variant.Prototype.ID,
                    ToggleMode = true,
                    Pressed = variant.Prototype == selected,
                    ClipText = true,
                };
                var target = variant.Prototype;
                button.OnPressed += _ => OnVariantSelected?.Invoke(target);
                _variants.AddChild(button);
            }
        }

        _info.SetMessage(BuildInfo(entry));
    }

    private FormattedMessage BuildInfo(ADTEntitySpawnEntry entry)
    {
        var prototype = entry.Prototype;
        var message = new FormattedMessage();

        AddLine(message, "adt-spawn-menu-details-category", GetCategoryPath(entry.Category));

        if (entry.ServiceCategory != null)
            AddLine(message, "adt-spawn-menu-details-service-category", GetCategoryPath(entry.ServiceCategory));

        if (prototype.Parents is { Length: > 0 })
            AddLine(message, "adt-spawn-menu-details-parents", string.Join(", ", prototype.Parents));

        var components = prototype.Components.Keys.OrderBy(k => k, StringComparer.Ordinal);
        AddLine(message, "adt-spawn-menu-details-components", string.Join(", ", components));

        var description = string.IsNullOrEmpty(prototype.Description)
            ? Loc.GetString("adt-spawn-menu-no-description")
            : prototype.Description;
        AddLine(message, "adt-spawn-menu-details-description", description);

        return message;
    }

    private static void AddLine(FormattedMessage message, string caption, string value)
    {
        if (!message.IsEmpty)
            message.PushNewline();

        message.PushColor(CaptionColor);
        message.AddText(Loc.GetString(caption));
        message.Pop();
        message.AddText(" ");
        message.AddText(value);
    }

    private static string GetCategoryPath(ADTSpawnCategoryNode? node)
    {
        if (node == null)
            return Loc.GetString("adt-spawn-menu-category-other");

        var names = new List<string>();
        while (node != null)
        {
            names.Add(node.Name);
            node = node.Parent;
        }

        names.Reverse();
        return string.Join(" / ", names);
    }

    private void CopyId()
    {
        if (_shown != null)
            _clipboard.SetText(_shown.ID);
    }
}
