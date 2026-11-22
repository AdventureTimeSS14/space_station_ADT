using System.Linq;
using System.Numerics;
using Content.Client.ADT.Polls.UI;
using Content.Shared.ADT.Polls;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;

namespace Content.Client.ADT.Polls;

[UsedImplicitly]
public sealed class PollUIController : UIController
{
    [Dependency] private IEntityNetworkManager _net = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private List<PollSummary> _polls = new();
    private readonly HashSet<int> _invited = new();

    private PollInviteWindow? _invite;
    private PollListWindow? _list;
    private PollAnswerWindow? _answer;

    public event Action? PollsChanged;

    public IReadOnlyList<PollSummary> Polls => _polls;

    public int UnansweredCount => _polls.Count(p => p.Targeted && !p.Answered);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PollListEvent>(OnList);
        SubscribeNetworkEvent<PollInviteEvent>(OnInvite);
        SubscribeNetworkEvent<PollContentEvent>(OnContent);
        SubscribeNetworkEvent<PollSubmitResultEvent>(OnSubmitResult);
    }

    private void OnList(PollListEvent ev, EntitySessionEventArgs args)
    {
        _polls = ev.Polls;

        _invited.RemoveWhere(id => !_polls.Any(p => p.Id == id && !p.Answered));

        _list?.SetPolls(_polls);
        _invite?.SetPolls(_polls.Where(p => _invited.Contains(p.Id)).ToList());

        if (_answer != null && _answer.IsOpen && _polls.All(p => p.Id != _answer.PollId))
            _answer.SetClosedByServer();

        PollsChanged?.Invoke();
    }

    private void OnInvite(PollInviteEvent ev, EntitySessionEventArgs args)
    {
        var added = false;

        foreach (var id in ev.PollIds)
        {
            if (_polls.Any(p => p.Id == id && !p.Answered))
                added |= _invited.Add(id);
        }

        if (!added && _invite is { IsOpen: true })
            return;

        if (_invited.Count == 0)
            return;

        if (_invite == null)
        {
            _invite = new PollInviteWindow();
            _invite.OpenRequested += OpenPoll;
            _invite.OnClose += () => _invited.Clear();
        }

        _invite.SetPolls(_polls.Where(p => _invited.Contains(p.Id)).ToList());

        if (!_invite.IsOpen)
            _invite.OpenCenteredAt(new Vector2(0.5f, 0.2f));
    }

    public void ToggleListWindow()
    {
        if (_list == null)
        {
            _list = new PollListWindow();
            _list.OpenRequested += OpenPoll;
        }

        if (_list.IsOpen)
        {
            _list.Close();
            return;
        }

        _list.SetPolls(_polls);
        _list.OpenCentered();
    }

    public void OpenPoll(int pollId)
    {
        _net.SendSystemNetworkMessage(new PollRequestEvent
        {
            PollId = pollId,
        });
    }

    private void OnContent(PollContentEvent ev, EntitySessionEventArgs args)
    {
        if (_answer == null)
        {
            _answer = new PollAnswerWindow();
            _answer.SubmitRequested += Submit;
        }

        _answer.SetPoll(ev.Poll, ev.Answered, _cfg.GetCVar(PollCVars.TextMaxLength));

        if (_answer.IsOpen)
            _answer.MoveToFront();
        else
            _answer.OpenCentered();

        if (_invited.Remove(ev.Poll.Id))
        {
            _invite?.SetPolls(_polls.Where(p => _invited.Contains(p.Id)).ToList());

            if (_invited.Count == 0)
                _invite?.Close();
        }
    }

    private void Submit(int pollId, List<PollAnswer> answers)
    {
        _net.SendSystemNetworkMessage(new PollSubmitEvent
        {
            PollId = pollId,
            Answers = answers,
        });
    }

    private void OnSubmitResult(PollSubmitResultEvent ev, EntitySessionEventArgs args)
    {
        if (_answer == null || _answer.PollId != ev.PollId)
            return;

        _answer.SetResult(ev.Success, Loc.GetString(ev.Message));
    }
}
