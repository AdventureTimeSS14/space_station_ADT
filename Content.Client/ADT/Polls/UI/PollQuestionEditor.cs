using System.Linq;
using Content.Shared.ADT.Polls;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Polls.UI;

public sealed class PollQuestionEditor : PanelContainer
{
    private readonly Label _index;
    private readonly LineEdit _text;
    private readonly OptionButton _type;
    private readonly CheckBox _required;
    private readonly BoxContainer _optionsBox;
    private readonly TextEdit _options;
    private readonly BoxContainer _ratingBox;
    private readonly LineEdit _ratingMin;
    private readonly LineEdit _ratingMax;
    private readonly Button _up;
    private readonly Button _down;
    private readonly Button _remove;

    public event Action<PollQuestionEditor, int>? MoveRequested;
    public event Action<PollQuestionEditor>? RemoveRequested;

    public PollQuestionEditor(PollQuestion question)
    {
        StyleClasses.Add("BackgroundDark");

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Margin = new Thickness(6),
        };

        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        _index = new Label
        {
            MinWidth = 24,
        };

        _text = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("adt-poll-editor-question-placeholder"),
            Text = question.Text,
        };

        _type = new OptionButton
        {
            MinWidth = 150,
        };

        var types = new[]
        {
            PollQuestionType.Single,
            PollQuestionType.Multiple,
            PollQuestionType.Rating,
            PollQuestionType.Text,
        };

        foreach (var type in types)
        {
            _type.AddItem(PollNames.QuestionType(type), (int) type);
        }

        _type.SelectId((int) question.Type);
        _type.OnItemSelected += args =>
        {
            _type.SelectId(args.Id);
            UpdateVisibility();
        };

        _up = new Button
        {
            Text = "^",
            StyleClasses = { "OpenRight" },
        };

        _down = new Button
        {
            Text = "v",
            StyleClasses = { "OpenBoth" },
        };

        _remove = new Button
        {
            Text = "X",
            StyleClasses = { "OpenLeft", "Caution" },
        };

        _up.OnPressed += _ => MoveRequested?.Invoke(this, -1);
        _down.OnPressed += _ => MoveRequested?.Invoke(this, 1);
        _remove.OnPressed += _ => RemoveRequested?.Invoke(this);

        header.AddChild(_index);
        header.AddChild(_text);
        header.AddChild(_type);
        header.AddChild(_up);
        header.AddChild(_down);
        header.AddChild(_remove);
        root.AddChild(header);

        _required = new CheckBox
        {
            Text = Loc.GetString("adt-poll-editor-required"),
            Pressed = question.Required,
        };

        root.AddChild(_required);

        _optionsBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };

        _optionsBox.AddChild(new Label
        {
            Text = Loc.GetString("adt-poll-editor-options"),
            FontColorOverride = Color.Gray,
        });

        _options = new TextEdit
        {
            MinHeight = 80,
            HorizontalExpand = true,
            TextRope = new Rope.Leaf(string.Join("\n", question.Options)),
        };

        _optionsBox.AddChild(new PanelContainer
        {
            StyleClasses = { "BackgroundDark" },
            Children = { _options },
        });

        root.AddChild(_optionsBox);

        _ratingBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        _ratingMin = new LineEdit
        {
            MinWidth = 50,
            Text = question.RatingMin.ToString(),
        };

        _ratingMax = new LineEdit
        {
            MinWidth = 50,
            Text = question.RatingMax.ToString(),
        };

        _ratingBox.AddChild(new Label
        {
            Text = Loc.GetString("adt-poll-editor-rating-from"),
        });

        _ratingBox.AddChild(_ratingMin);

        _ratingBox.AddChild(new Label
        {
            Text = Loc.GetString("adt-poll-editor-rating-to"),
        });

        _ratingBox.AddChild(_ratingMax);
        root.AddChild(_ratingBox);

        AddChild(root);
        UpdateVisibility();
    }

    public void SetIndex(int index, int count)
    {
        _index.Text = $"{index + 1}.";
        _up.Disabled = index == 0;
        _down.Disabled = index == count - 1;
    }

    public void SetLocked(bool locked)
    {
        _text.Editable = !locked;
        _type.Disabled = locked;
        _required.Disabled = locked;
        _options.Editable = !locked;
        _ratingMin.Editable = !locked;
        _ratingMax.Editable = !locked;
        _up.Disabled |= locked;
        _down.Disabled |= locked;
        _remove.Disabled = locked;
    }

    public PollQuestion Build()
    {
        var question = new PollQuestion
        {
            Text = _text.Text.Trim(),
            Type = (PollQuestionType) _type.SelectedId,
            Required = _required.Pressed,
        };

        switch (question.Type)
        {
            case PollQuestionType.Single:
            case PollQuestionType.Multiple:
                question.Options = Rope.Collapse(_options.TextRope)
                    .Split('\n')
                    .Select(o => o.Trim())
                    .Where(o => o.Length > 0)
                    .ToList();
                break;

            case PollQuestionType.Rating:
                if (int.TryParse(_ratingMin.Text, out var min))
                    question.RatingMin = min;

                if (int.TryParse(_ratingMax.Text, out var max))
                    question.RatingMax = max;

                break;
        }

        return question;
    }

    private void UpdateVisibility()
    {
        var type = (PollQuestionType) _type.SelectedId;

        _optionsBox.Visible = type is PollQuestionType.Single or PollQuestionType.Multiple;
        _ratingBox.Visible = type == PollQuestionType.Rating;
    }
}
