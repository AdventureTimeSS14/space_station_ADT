using System.Threading.Tasks;
using Content.Server.ADT.Polls;
using Content.Shared.ADT.Polls;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<List<PollData>> GetActivePollsAsync();
    Task<List<PollAdminEntry>> GetPollAdminEntriesAsync();
    Task<PollData?> GetPollAsync(int pollId);
    Task<int> GetPollResponseCountAsync(int pollId);
    Task<PollData> CreatePollAsync(PollData poll);
    Task<bool> UpdatePollAsync(PollData poll);
    Task<bool> SetPollClosedAsync(int pollId, bool closed);
    Task<bool> DeletePollAsync(int pollId);
    Task<int> WipePollResponsesAsync(int pollId);
    Task<PollUserState> GetPollUserStateAsync(Guid userId);
    Task AddPollPendingAsync(List<PollPendingEntry> entries);
    Task<bool> SubmitPollResponseAsync(PollResponseView response, int pollId);
    Task<List<PollResponseView>> GetPollResponsesAsync(int pollId);
    Task<int> CleanupPollsAsync(DateTime cutoff);
}

public sealed partial class ServerDbManager
{
    public Task<List<PollData>> GetActivePollsAsync()
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetActivePolls());
    }

    public Task<List<PollAdminEntry>> GetPollAdminEntriesAsync()
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPollAdminEntries());
    }

    public Task<PollData?> GetPollAsync(int pollId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPoll(pollId));
    }

    public Task<int> GetPollResponseCountAsync(int pollId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPollResponseCount(pollId));
    }

    public Task<PollData> CreatePollAsync(PollData poll)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.CreatePoll(poll));
    }

    public Task<bool> UpdatePollAsync(PollData poll)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.UpdatePoll(poll));
    }

    public Task<bool> SetPollClosedAsync(int pollId, bool closed)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SetPollClosed(pollId, closed));
    }

    public Task<bool> DeletePollAsync(int pollId)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.DeletePoll(pollId));
    }

    public Task<int> WipePollResponsesAsync(int pollId)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.WipePollResponses(pollId));
    }

    public Task<PollUserState> GetPollUserStateAsync(Guid userId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPollUserState(userId));
    }

    public Task AddPollPendingAsync(List<PollPendingEntry> entries)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.AddPollPending(entries));
    }

    public Task<bool> SubmitPollResponseAsync(PollResponseView response, int pollId)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SubmitPollResponse(response, pollId));
    }

    public Task<List<PollResponseView>> GetPollResponsesAsync(int pollId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPollResponses(pollId));
    }

    public Task<int> CleanupPollsAsync(DateTime cutoff)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.CleanupPolls(cutoff));
    }
}
