using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.ADT.Polls;
using Content.Shared.Administration;
using Content.Shared.Eui;

namespace Content.Server.ADT.Polls;

public sealed class PollPanelEui : BaseEui
{
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IEntitySystemManager _systems = default!;
    [Dependency] private ILogManager _log = default!;

    private readonly ISawmill _sawmill;
    private readonly ADTPollSystem _polls;

    private List<PollAdminEntry> _entries = new();
    private PollStats? _stats;
    private string _status = string.Empty;

    public PollPanelEui()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("polls.adt.eui");
        _polls = _systems.GetEntitySystem<ADTPollSystem>();
    }

    public override void Opened()
    {
        base.Opened();

        _admins.OnPermsChanged += OnPermsChanged;
        Reload();
    }

    public override void Closed()
    {
        base.Closed();

        _admins.OnPermsChanged -= OnPermsChanged;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        StateDirty();
    }

    private bool HasPermission()
    {
        return _admins.HasAdminFlag(Player, AdminFlags.Host);
    }

    public override EuiStateBase GetNewState()
    {
        if (!HasPermission())
        {
            return new PollPanelEuiState
            {
                HasPermission = false,
            };
        }

        return new PollPanelEuiState
        {
            HasPermission = true,
            ServerName = _polls.ServerName,
            Polls = _entries,
            Stats = _stats,
            Status = _status,
        };
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is CloseEuiMessage)
            return;

        if (!HasPermission())
            return;

        switch (msg)
        {
            case PollPanelEuiMsg.Refresh:
                Reload();
                break;

            case PollPanelEuiMsg.Save m:
                Save(m.Poll);
                break;

            case PollPanelEuiMsg.SetClosed m:
                SetClosed(m.PollId, m.Closed);
                break;

            case PollPanelEuiMsg.Delete m:
                Delete(m.PollId);
                break;

            case PollPanelEuiMsg.Wipe m:
                Wipe(m.PollId);
                break;

            case PollPanelEuiMsg.RequestStats m:
                LoadStats(m.PollId, m.GroupBy, m.Page);
                break;
        }
    }

    private async void Reload(string? status = null)
    {
        try
        {
            _entries = await _polls.GetAdminEntries();

            if (_stats != null)
                _stats = await _polls.BuildStats(_stats.PollId, _stats.GroupBy, _stats.PageIndex);

            if (status != null)
                _status = status;
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось загрузить опросы: {ex}");
            _status = "adt-poll-error-save";
        }

        if (!IsShutDown)
            StateDirty();
    }

    private async void Save(PollData poll)
    {
        try
        {
            var (saved, message) = await _polls.SavePoll(poll, Player);

            if (saved != null && _stats?.PollId == saved.Id)
                _stats = null;

            Reload(message);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось сохранить опрос: {ex}");
            Reload("adt-poll-error-save");
        }
    }

    private async void SetClosed(int pollId, bool closed)
    {
        try
        {
            var ok = await _polls.SetPollClosed(pollId, closed, Player);
            Reload(ok ? "adt-poll-panel-done" : "adt-poll-error-not-found");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось изменить опрос {pollId}: {ex}");
            Reload("adt-poll-error-save");
        }
    }

    private async void Delete(int pollId)
    {
        try
        {
            var ok = await _polls.DeletePoll(pollId, Player);

            if (_stats?.PollId == pollId)
                _stats = null;

            Reload(ok ? "adt-poll-panel-done" : "adt-poll-error-not-found");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось удалить опрос {pollId}: {ex}");
            Reload("adt-poll-error-save");
        }
    }

    private async void Wipe(int pollId)
    {
        try
        {
            await _polls.WipeResponses(pollId, Player);
            Reload("adt-poll-panel-done");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось стереть ответы опроса {pollId}: {ex}");
            Reload("adt-poll-error-save");
        }
    }

    private async void LoadStats(int pollId, PollGroupBy groupBy, int page)
    {
        try
        {
            _stats = await _polls.BuildStats(pollId, groupBy, page);
            _status = _stats == null ? "adt-poll-error-not-found" : string.Empty;
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось собрать статистику опроса {pollId}: {ex}");
            _status = "adt-poll-error-save";
        }

        if (!IsShutDown)
            StateDirty();
    }
}
