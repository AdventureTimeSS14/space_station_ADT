using System.Linq;
using System.Threading.Tasks;
using Content.Shared.ADT.Polls;
using Robust.Shared.Player;

namespace Content.Server.ADT.Polls;

public sealed partial class ADTPollSystem
{
    public const int StatsPageSize = 25;

    public Task<List<PollAdminEntry>> GetAdminEntries()
    {
        return _db.GetPollAdminEntriesAsync();
    }

    public async Task<(PollData? Poll, string Message)> SavePoll(PollData poll, ICommonSession admin)
    {
        var error = PollValidation.ValidatePoll(poll);

        if (error != null)
            return (null, error);

        PollData saved;
        var message = "adt-poll-panel-saved";

        if (poll.Id == 0)
        {
            poll.CreatedBy = admin.UserId.UserId;
            saved = await _db.CreatePollAsync(poll);
        }
        else
        {
            var existing = await _db.GetPollAsync(poll.Id);

            if (existing == null)
                return (null, "adt-poll-error-not-found");

            if (await _db.GetPollResponseCountAsync(poll.Id) > 0)
            {
                poll.Questions = existing.Questions;
                message = "adt-poll-panel-saved-questions-locked";
            }

            poll.CreatedBy = existing.CreatedBy;
            poll.CreatedAt = existing.CreatedAt;

            if (!await _db.UpdatePollAsync(poll))
                return (null, "adt-poll-error-not-found");

            saved = poll;
        }

        ApplyToCache(saved);

        return (saved, message);
    }

    public async Task<bool> SetPollClosed(int pollId, bool closed, ICommonSession? admin)
    {
        if (!await _db.SetPollClosedAsync(pollId, closed))
            return false;

        var poll = await _db.GetPollAsync(pollId);

        if (poll != null)
            ApplyToCache(poll);

        return true;
    }

    public async Task<bool> DeletePoll(int pollId, ICommonSession? admin)
    {
        if (!await _db.DeletePollAsync(pollId))
            return false;

        _active.Remove(pollId);

        foreach (var state in _sessions.Values)
        {
            state.Answered.Remove(pollId);
            state.Targeted.Remove(pollId);
        }

        SendListToAll();
        return true;
    }

    public async Task<int> WipeResponses(int pollId, ICommonSession? admin)
    {
        var removed = await _db.WipePollResponsesAsync(pollId);

        foreach (var state in _sessions.Values)
        {
            state.Answered.Remove(pollId);
        }

        SendListToAll();
        return removed;
    }

    private void ApplyToCache(PollData poll)
    {
        var wasActive = _active.ContainsKey(poll.Id);

        if (poll.IsActive(DateTime.UtcNow) && poll.RunsOn(_serverName))
            _active[poll.Id] = poll;
        else
            _active.Remove(poll.Id);

        if (!wasActive && _active.ContainsKey(poll.Id) && poll.ShowMode == PollShowMode.Immediately && _enabled)
            TargetAllOnline(poll);

        SendListToAll();
    }

    public async Task<PollStats?> BuildStats(int pollId, PollGroupBy groupBy, int page)
    {
        var poll = await _db.GetPollAsync(pollId);

        if (poll == null)
            return null;

        var responses = await _db.GetPollResponsesAsync(pollId);

        var stats = new PollStats
        {
            PollId = pollId,
            Total = responses.Count,
            Targeted = responses.Count(r => r.Targeted),
            GroupBy = groupBy,
        };

        for (var i = 0; i < poll.Questions.Count; i++)
        {
            stats.Questions.Add(BuildQuestionStats(poll.Questions[i], i, responses, groupBy));
        }

        stats.PageCount = Math.Max(1, (responses.Count + StatsPageSize - 1) / StatsPageSize);
        stats.PageIndex = Math.Clamp(page, 0, stats.PageCount - 1);

        stats.Page = Enumerable.Reverse(responses)
            .Skip(stats.PageIndex * StatsPageSize)
            .Take(StatsPageSize)
            .ToList();

        return stats;
    }

    private static PollQuestionStats BuildQuestionStats(PollQuestion question, int index, List<PollResponseView> responses, PollGroupBy groupBy)
    {
        var size = question.Type switch
        {
            PollQuestionType.Single => question.Options.Count,
            PollQuestionType.Multiple => question.Options.Count,
            PollQuestionType.Rating => question.RatingMax - question.RatingMin + 1,
            _ => 0,
        };

        var result = new PollQuestionStats
        {
            Counts = new int[size],
        };

        foreach (var response in responses)
        {
            if (index >= response.Answers.Count)
                continue;

            var answer = response.Answers[index];

            if (answer.IsEmpty())
                continue;

            result.Answered++;

            if (size == 0)
                continue;

            var buckets = new List<int>();

            if (answer.Options != null)
            {
                foreach (var option in answer.Options)
                {
                    if (option >= 0 && option < size)
                        buckets.Add(option);
                }
            }

            if (answer.Rating is { } rating)
            {
                var bucket = rating - question.RatingMin;

                if (bucket >= 0 && bucket < size)
                    buckets.Add(bucket);
            }

            foreach (var bucket in buckets)
            {
                result.Counts[bucket]++;
            }

            if (groupBy == PollGroupBy.None)
                continue;

            foreach (var group in GetGroups(response.Snapshot, groupBy))
            {
                if (!result.Groups.TryGetValue(group, out var counts))
                {
                    counts = new int[size];
                    result.Groups[group] = counts;
                }

                foreach (var bucket in buckets)
                {
                    counts[bucket]++;
                }
            }
        }

        return result;
    }

    private static IEnumerable<string> GetGroups(PollSnapshot snapshot, PollGroupBy groupBy)
    {
        var source = groupBy switch
        {
            PollGroupBy.Department => snapshot.Departments,
            PollGroupBy.Job => snapshot.Jobs,
            PollGroupBy.Species => snapshot.Species,
            _ => new HashSet<string>(),
        };

        if (source.Count == 0)
        {
            return new[]
            {
                string.Empty,
            };
        }

        return source;
    }
}
