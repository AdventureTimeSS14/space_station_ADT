using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Polls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Polls.UI;

public sealed class PollAnswerWindow : DefaultWindow
{
    private readonly RichTextLabel _description;
    private readonly BoxContainer _questions;
    private readonly Label _status;
    private readonly Button _submit;

    private readonly List<QuestionInput> _inputs = new();

    private PollData _poll = new();
    private int _textMaxLength;
    private bool _locked;

    public int PollId => _poll.Id;

    public event Action<int, List<PollAnswer>>? SubmitRequested;

    private sealed class QuestionInput
    {
        public required PollQuestion Question;
        public List<BaseButton> Buttons = new();
        public TextEdit? Text;
    }

    public PollAnswerWindow()
    {
        MinSize = new Vector2(520, 400);
        SetSize = new Vector2(600, 620);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            Margin = new Thickness(6),
        };

        _description = new RichTextLabel();

        _questions = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 10,
        };

        var body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 10,
            Margin = new Thickness(0, 0, 8, 0),
        };

        body.AddChild(_description);
        body.AddChild(_questions);

        root.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { body },
        });

        var footer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };

        _status = new Label
        {
            HorizontalExpand = true,
            ClipText = true,
        };

        _submit = new Button
        {
            Text = Loc.GetString("adt-poll-answer-submit"),
            StyleClasses = { "Caution" },
        };

        _submit.OnPressed += _ => Submit();

        footer.AddChild(_status);
        footer.AddChild(_submit);
        root.AddChild(footer);

        Contents.AddChild(root);
    }

    public void SetPoll(PollData poll, bool answered, int textMaxLength)
    {
        _poll = poll;
        _textMaxLength = textMaxLength;
        _locked = answered;

        Title = poll.Title;
        _description.SetMessage(FormattedMessage.FromMarkupPermissive(poll.Description));
        _description.Visible = poll.Description.Length > 0;

        _questions.RemoveAllChildren();
        _inputs.Clear();

        for (var i = 0; i < poll.Questions.Count; i++)
        {
            _questions.AddChild(BuildQuestion(i, poll.Questions[i]));
        }

        if (answered)
            SetStatus(Loc.GetString("adt-poll-error-already-answered"), Color.Gray);
        else
            SetStatus(string.Empty, Color.White);

        UpdateSubmit();
    }

    public void SetResult(bool success, string message)
    {
        if (success)
            _locked = true;

        SetStatus(message, success ? Color.LightGreen : Color.OrangeRed);
        UpdateSubmit();
    }

    public void SetClosedByServer()
    {
        if (_locked)
            return;

        _locked = true;
        SetStatus(Loc.GetString("adt-poll-error-closed"), Color.OrangeRed);
        UpdateSubmit();
    }

    private void SetStatus(string text, Color color)
    {
        _status.Text = text;
        _status.FontColorOverride = color;
    }

    private Control BuildQuestion(int index, PollQuestion question)
    {
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };

        var header = $"{index + 1}. {question.Text}";

        if (question.Required)
            header += " *";

        box.AddChild(new Label
        {
            Text = header,
            StyleClasses = { "LabelHeading" },
        });

        var input = new QuestionInput
        {
            Question = question,
        };

        switch (question.Type)
        {
            case PollQuestionType.Single:
            case PollQuestionType.Multiple:
            {
                var group = question.Type == PollQuestionType.Single ? new ButtonGroup() : null;

                foreach (var option in question.Options)
                {
                    var check = new CheckBox
                    {
                        Text = option,
                        Group = group,
                    };

                    check.OnToggled += _ => UpdateSubmit();
                    input.Buttons.Add(check);
                    box.AddChild(check);
                }

                break;
            }

            case PollQuestionType.Rating:
            {
                var group = new ButtonGroup();

                var row = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    SeparationOverride = 2,
                };

                for (var value = question.RatingMin; value <= question.RatingMax; value++)
                {
                    var button = new Button
                    {
                        Text = value.ToString(),
                        ToggleMode = true,
                        Group = group,
                        MinWidth = 36,
                    };

                    button.OnToggled += _ => UpdateSubmit();
                    input.Buttons.Add(button);
                    row.AddChild(button);
                }

                box.AddChild(row);
                break;
            }

            case PollQuestionType.Text:
            {
                var text = new TextEdit
                {
                    MinHeight = 70,
                    HorizontalExpand = true,
                    Placeholder = new Rope.Leaf(Loc.GetString("adt-poll-answer-text-placeholder")),
                };

                text.OnTextChanged += _ => UpdateSubmit();
                input.Text = text;

                box.AddChild(new PanelContainer
                {
                    StyleClasses = { "BackgroundDark" },
                    Children = { text },
                });

                break;
            }
        }

        _inputs.Add(input);
        return box;
    }

    private List<PollAnswer> CollectAnswers()
    {
        var answers = new List<PollAnswer>(_inputs.Count);

        foreach (var input in _inputs)
        {
            var answer = new PollAnswer();

            switch (input.Question.Type)
            {
                case PollQuestionType.Single:
                case PollQuestionType.Multiple:
                {
                    var selected = new List<int>();

                    for (var i = 0; i < input.Buttons.Count; i++)
                    {
                        if (input.Buttons[i].Pressed)
                            selected.Add(i);
                    }

                    if (selected.Count > 0)
                        answer.Options = selected;

                    break;
                }

                case PollQuestionType.Rating:
                    for (var i = 0; i < input.Buttons.Count; i++)
                    {
                        if (input.Buttons[i].Pressed)
                            answer.Rating = input.Question.RatingMin + i;
                    }

                    break;

                case PollQuestionType.Text:
                    if (input.Text != null)
                    {
                        var text = Rope.Collapse(input.Text.TextRope).Trim();

                        if (text.Length > 0)
                            answer.Text = text;
                    }

                    break;
            }

            answers.Add(answer);
        }

        return answers;
    }

    private void UpdateSubmit()
    {
        if (_locked)
        {
            _submit.Disabled = true;

            foreach (var input in _inputs)
            {
                foreach (var button in input.Buttons)
                {
                    button.Disabled = true;
                }

                if (input.Text != null)
                    input.Text.Editable = false;
            }

            return;
        }

        var answers = CollectAnswers();
        var ready = true;
        var tooLong = false;

        for (var i = 0; i < answers.Count; i++)
        {
            if (_inputs[i].Question.Required && answers[i].IsEmpty())
                ready = false;

            if (answers[i].Text is { } text && text.Length > _textMaxLength)
                tooLong = true;
        }

        if (tooLong)
            SetStatus(Loc.GetString("adt-poll-error-text-too-long"), Color.OrangeRed);
        else if (_status.FontColorOverride == Color.OrangeRed)
            SetStatus(string.Empty, Color.White);

        _submit.Disabled = !ready || tooLong;
    }

    private void Submit()
    {
        if (_locked)
            return;

        _submit.Disabled = true;
        SubmitRequested?.Invoke(_poll.Id, CollectAnswers());
    }
}
