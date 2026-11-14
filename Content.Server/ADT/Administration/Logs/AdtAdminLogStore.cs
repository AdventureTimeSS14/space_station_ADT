using System.Buffers;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Shared.ADT.CCVar;
using Content.Shared.CCVar;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.Configuration;

namespace Content.Server.ADT.Administration.Logs;

public sealed class AdtAdminLogStore
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    private readonly object _lock = new();
    private readonly object _builderLock = new();
    private readonly AdtLogChunkBuilder _builder = new();

    private readonly Dictionary<int, RoundCache> _rounds = new();
    private readonly List<int> _roundOrder = new();

    private readonly HashSet<int> _legacyRounds = new();

    private readonly int _logTypeCount = Enum.GetValues<LogType>().Length;

    private int _currentRound;
    private int _cachedBytes;

    private bool _enabledCVar;
    private bool _synchronousDb;

    public bool Enabled { get; private set; }
    public bool WriteLegacy { get; private set; }

    private int _chunkMaxLogs;
    private int _chunkMaxBytes;
    private int _compression;
    private int _cacheRounds;
    private int _cacheBytes;
    private int _fetchBatch;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("admin.logs.chunks");

        _cfg.OnValueChanged(ADTAdminLogCVars.AdminLogsChunksEnabled, value => _enabledCVar = value, true);
        Enabled = _enabledCVar;
        _cfg.OnValueChanged(CCVars.DatabaseSynchronous, value => _synchronousDb = value, true);
        _cfg.OnValueChanged(ADTAdminLogCVars.AdminLogsWriteLegacy, value => WriteLegacy = value, true);
        _cfg.OnValueChanged(ADTAdminLogCVars.AdminLogsChunkMaxLogs, value => _chunkMaxLogs = value, true);
        _cfg.OnValueChanged(ADTAdminLogCVars.AdminLogsChunkMaxBytes, value => _chunkMaxBytes = value, true);
        _cfg.OnValueChanged(ADTAdminLogCVars.AdminLogsChunkCompression, value => _compression = value, true);
        _cfg.OnValueChanged(ADTAdminLogCVars.AdminLogsCacheRounds, value => _cacheRounds = value, true);
        _cfg.OnValueChanged(ADTAdminLogCVars.AdminLogsCacheBytes, value => _cacheBytes = value, true);
        _cfg.OnValueChanged(ADTAdminLogCVars.AdminLogsFetchBatch, value => _fetchBatch = value, true);
    }

    public bool StartRound(int roundId)
    {
        Enabled = _enabledCVar;

        if (!Enabled)
            return false;

        lock (_lock)
        {
            _currentRound = roundId;
            _legacyRounds.Remove(roundId);
            GetOrCreateRound(roundId, true);
            TrimRounds();
        }

        return true;
    }

    public void AppendLive(int roundId, SharedAdminLog log)
    {
        lock (_lock)
        {
            var round = GetOrCreateRound(roundId, true);
            round.Tail.Add(log);
        }
    }

    #region Запись
    public async Task Save(List<AdminLog> logs)
    {
        if (logs.Count == 0)
            return;

        var byRound = new Dictionary<int, List<AdminLog>>();

        foreach (var log in logs)
        {
            if (!byRound.TryGetValue(log.RoundId, out var list))
            {
                list = new List<AdminLog>();
                byRound.Add(log.RoundId, list);
            }

            list.Add(log);
        }

        foreach (var (roundId, roundLogs) in byRound)
        {
            await SaveRound(roundId, roundLogs);
        }
    }

    private async Task SaveRound(int roundId, List<AdminLog> logs)
    {
        logs.Sort(static (a, b) => a.Id.CompareTo(b.Id));

        var nextIndex = await GetNextChunkIndex(roundId);

        List<AdtAdminLogChunk> chunks;
        if (_synchronousDb)
        {
            chunks = BuildChunks(roundId, nextIndex, logs);
        }
        else
        {
            chunks = await Task.Run(() => BuildChunks(roundId, nextIndex, logs));
        }
        if (chunks.Count == 0)
            return;

        var packed = new List<CachedChunk>(chunks.Count);

        lock (_lock)
        {
            var round = GetOrCreateRound(roundId, true);
            round.NextChunkIndex = chunks[^1].ChunkIndex + 1;

            foreach (var chunk in chunks)
            {
                var cached = new CachedChunk
                {
                    Header = AdtLogChunkHeader.FromEntity(chunk),
                    Payload = chunk.Payload,
                };

                packed.Add(cached);
                round.Chunks.Add(cached);
                _cachedBytes += chunk.Payload.Length;
            }

            _legacyRounds.Remove(roundId);
        }

        var saved = await _db.AddAdminLogChunks(chunks);

        lock (_lock)
        {
            if (!saved)
            {
                ForgetChunks(roundId, packed);
                throw new InvalidOperationException($"Не удалось сохранить {chunks.Count} чанков админ-логов раунда {roundId}.");
            }

            foreach (var cached in packed)
            {
                cached.Persisted = true;
            }

            DropTail(roundId, logs);
            TrimCache();
        }
    }

    private void ForgetChunks(int roundId, List<CachedChunk> packed)
    {
        if (!_rounds.TryGetValue(roundId, out var round))
            return;

        foreach (var cached in packed)
        {
            if (round.Chunks.Remove(cached))
                _cachedBytes -= cached.Payload?.Length ?? 0;
        }
    }

    private List<AdtAdminLogChunk> BuildChunks(int roundId, int nextIndex, List<AdminLog> logs)
    {
        var chunks = new List<AdtAdminLogChunk>();

        lock (_builderLock)
        {
            _builder.Reset(roundId, nextIndex);

            foreach (var log in logs)
            {
                _builder.Append(log);

                if (_builder.Count < _chunkMaxLogs && _builder.RawSize < _chunkMaxBytes)
                    continue;

                chunks.Add(_builder.Finish(_compression));
                nextIndex++;
                _builder.Reset(roundId, nextIndex);
            }

            if (_builder.Count > 0)
                chunks.Add(_builder.Finish(_compression));
        }

        return chunks;
    }

    private async Task<int> GetNextChunkIndex(int roundId)
    {
        lock (_lock)
        {
            var cached = GetOrCreateRound(roundId, true);

            if (cached.NextChunkIndex >= 0)
                return cached.NextChunkIndex;
        }

        var last = await _db.GetLastAdminLogChunkIndex(roundId);
        lock (_lock)
        {
            var cached = GetOrCreateRound(roundId, true);

            if (cached.NextChunkIndex < 0)
                cached.NextChunkIndex = last + 1;

            return cached.NextChunkIndex;
        }
    }

    private void DropTail(int roundId, List<AdminLog> saved)
    {
        if (!_rounds.TryGetValue(roundId, out var round) || round.Tail.Count == 0)
            return;

        var ids = new HashSet<int>(saved.Count);

        foreach (var log in saved)
        {
            ids.Add(log.Id);
        }

        round.Tail.RemoveAll(log => ids.Contains(log.Id));
    }

    #endregion

    #region Чтение
    public async Task<List<SharedAdminLog>?> TryGetLogs(LogFilter? filter, Func<List<SharedAdminLog>>? listProvider)
    {
        if (!Enabled || filter?.Round is not { } roundId)
            return null;

        var round = await EnsureRound(roundId, filter.CancellationToken);

        if (round == null)
            return null;

        List<AdtLogChunkHeader> candidates;
        List<SharedAdminLog>? tail;
        var payloads = new Dictionary<int, byte[]>();

        lock (_lock)
        {
            candidates = new List<AdtLogChunkHeader>(round.Chunks.Count);
            tail = round.Tail.Count == 0 ? null : new List<SharedAdminLog>(round.Tail);

            foreach (var chunk in round.Chunks)
            {
                if (!chunk.Persisted || !MatchesHeader(chunk.Header, filter))
                    continue;

                candidates.Add(chunk.Header);

                if (chunk.Payload != null)
                    payloads[chunk.Header.ChunkIndex] = chunk.Payload;
            }
        }

        var results = listProvider?.Invoke() ?? new List<SharedAdminLog>();
        var limit = filter.Limit ?? int.MaxValue;

        if (limit <= 0)
            return results;

        var descending = filter.DateOrder == DateOrder.Descending;

        if (descending && tail != null)
            TakeFromTail(tail, filter, results, limit, true);

        if (results.Count < limit && candidates.Count > 0)
            await ScanChunks(roundId, candidates, payloads, filter, results, limit, descending);

        if (!descending && tail != null && results.Count < limit)
            TakeFromTail(tail, filter, results, limit, false);

        return results;
    }

    public async Task<int?> TryCountLogs(int roundId)
    {
        if (!Enabled)
            return null;

        var round = await EnsureRound(roundId, default);

        if (round == null)
            return null;

        lock (_lock)
        {
            var count = round.Tail.Count;

            foreach (var chunk in round.Chunks)
            {
                if (chunk.Persisted)
                    count += chunk.Header.LogCount;
            }

            return count;
        }
    }

    private async Task<RoundCache?> EnsureRound(int roundId, CancellationToken cancel)
    {
        lock (_lock)
        {
            if (_legacyRounds.Contains(roundId))
                return null;

            if (_rounds.TryGetValue(roundId, out var known) && known.HeadersLoaded)
            {
                Touch(roundId);
                return known;
            }
        }

        var headers = await _db.GetAdminLogChunkHeaders(roundId, cancel);

        lock (_lock)
        {
            if (headers.Count == 0 && roundId != _currentRound)
            {
                _legacyRounds.Add(roundId);
                return null;
            }

            var round = GetOrCreateRound(roundId, false);

            if (!round.HeadersLoaded)
            {
                foreach (var header in headers)
                {
                    round.Chunks.Add(new CachedChunk
                    {
                        Header = header,
                        Persisted = true,
                    });
                }

                round.HeadersLoaded = true;
            }

            TrimRounds();

            return round;
        }
    }

    private async Task ScanChunks(
        int roundId,
        List<AdtLogChunkHeader> candidates,
        Dictionary<int, byte[]> payloads,
        LogFilter filter,
        List<SharedAdminLog> results,
        int limit,
        bool descending)
    {
        if (descending)
            candidates.Reverse();

        var maxRaw = 0;
        var maxPlayers = 1;

        foreach (var header in candidates)
        {
            maxRaw = Math.Max(maxRaw, header.RawSize);
            maxPlayers = Math.Max(maxPlayers, header.Players.Length);
        }

        var rawBuffer = ArrayPool<byte>.Shared.Rent(maxRaw);
        var playerBuffer = ArrayPool<int>.Shared.Rent(maxPlayers);
        var charBuffer = filter.Search == null ? Array.Empty<char>() : ArrayPool<char>.Shared.Rent(8192);
        var scratch = new List<SharedAdminLog>();

        try
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                if (filter.CancellationToken.IsCancellationRequested)
                    return;

                var header = candidates[i];

                if (!payloads.TryGetValue(header.ChunkIndex, out var payload))
                {
                    await FetchPayloads(roundId, candidates, payloads, i, filter.CancellationToken);

                    if (!payloads.TryGetValue(header.ChunkIndex, out payload))
                    {
                        _sawmill.Warning($"Чанк {roundId}/{header.ChunkIndex} не найден в базе.");
                        continue;
                    }
                }

                var remaining = limit - results.Count;

                if (remaining <= 0)
                    return;

                scratch.Clear();

                try
                {
                    var size = AdtLogChunkFormat.Decompress(payload, rawBuffer.AsSpan(0, header.RawSize));

                    if (descending)
                    {
                        var total = CountMatches(header, rawBuffer.AsSpan(0, size), playerBuffer, charBuffer, filter);
                        var skip = Math.Max(0, total - remaining);

                        CollectMatches(header, rawBuffer.AsSpan(0, size), playerBuffer, charBuffer, filter, skip, remaining, scratch);
                    }
                    else
                    {
                        CollectMatches(header, rawBuffer.AsSpan(0, size), playerBuffer, charBuffer, filter, 0, remaining, scratch);
                    }
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Не удалось разобрать чанк {roundId}/{header.ChunkIndex}: {e}");
                    continue;
                }

                if (descending)
                {
                    for (var j = scratch.Count - 1; j >= 0; j--)
                    {
                        results.Add(scratch[j]);
                    }
                }
                else
                {
                    results.AddRange(scratch);
                }

                if (results.Count >= limit)
                    return;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rawBuffer);
            ArrayPool<int>.Shared.Return(playerBuffer);

            if (charBuffer.Length > 0)
                ArrayPool<char>.Shared.Return(charBuffer);
        }
    }

    private async Task FetchPayloads(
        int roundId,
        List<AdtLogChunkHeader> candidates,
        Dictionary<int, byte[]> payloads,
        int from,
        CancellationToken cancel)
    {
        var indices = new List<int>();

        for (var i = from; i < candidates.Count && indices.Count < Math.Max(1, _fetchBatch); i++)
        {
            if (!payloads.ContainsKey(candidates[i].ChunkIndex))
                indices.Add(candidates[i].ChunkIndex);
        }

        if (indices.Count == 0)
            return;

        var keep = new HashSet<int>(indices);
        foreach (var index in payloads.Keys.ToArray())
        {
            if (!keep.Contains(index) && !IsCachedPayload(roundId, index))
                payloads.Remove(index);
        }

        var fetched = await _db.GetAdminLogChunkPayloads(roundId, indices.ToArray(), cancel);

        foreach (var (index, payload) in fetched)
        {
            payloads[index] = payload;
        }
    }

    private bool IsCachedPayload(int roundId, int chunkIndex)
    {
        lock (_lock)
        {
            if (!_rounds.TryGetValue(roundId, out var round))
                return false;

            foreach (var chunk in round.Chunks)
            {
                if (chunk.Header.ChunkIndex == chunkIndex)
                    return chunk.Payload != null;
            }

            return false;
        }
    }

    #endregion

    #region Фильтрация

    private bool MatchesHeader(AdtLogChunkHeader header, LogFilter filter)
    {
        if (filter.Before != null && header.FirstDate >= filter.Before)
            return false;

        if (filter.After != null && header.LastDate <= filter.After)
            return false;

        if (filter.LastLogId != null)
        {
            if (filter.DateOrder == DateOrder.Descending && header.FirstLogId >= filter.LastLogId)
                return false;

            if (filter.DateOrder == DateOrder.Ascending && header.LastLogId <= filter.LastLogId)
                return false;
        }

        if (filter.Impacts != null)
        {
            var found = false;

            foreach (var impact in filter.Impacts)
            {
                if (!header.HasImpact(impact))
                    continue;

                found = true;
                break;
            }

            if (!found)
                return false;
        }

        if (filter.Types != null && filter.Types.Count != _logTypeCount)
        {
            var found = false;

            foreach (var type in filter.Types)
            {
                if (!header.HasType(type))
                    continue;

                found = true;
                break;
            }

            if (!found)
                return false;
        }

        if (!filter.IncludePlayers)
            return header.HasNonPlayerLogs;

        if (filter.AnyPlayers != null &&
            !header.HasAnyPlayer(filter.AnyPlayers) &&
            !(filter.IncludeNonPlayers && header.HasNonPlayerLogs))
        {
            return false;
        }

        return true;
    }

    private int CountMatches(
        AdtLogChunkHeader header,
        ReadOnlySpan<byte> raw,
        int[] playerBuffer,
        char[] charBuffer,
        LogFilter filter)
    {
        var scanner = new AdtLogChunkScanner(raw, playerBuffer);
        var count = 0;

        while (scanner.MoveNext())
        {
            if (Matches(header, ref scanner, charBuffer, filter))
                count++;
        }

        return count;
    }

    private void CollectMatches(
        AdtLogChunkHeader header,
        ReadOnlySpan<byte> raw,
        int[] playerBuffer,
        char[] charBuffer,
        LogFilter filter,
        int skip,
        int take,
        List<SharedAdminLog> output)
    {
        var scanner = new AdtLogChunkScanner(raw, playerBuffer);

        while (scanner.MoveNext())
        {
            if (!Matches(header, ref scanner, charBuffer, filter))
                continue;

            if (skip > 0)
            {
                skip--;
                continue;
            }

            var players = scanner.PlayerCount == 0
                ? Array.Empty<Guid>()
                : new Guid[scanner.PlayerCount];

            for (var i = 0; i < players.Length; i++)
            {
                players[i] = PlayerAt(header, scanner.Players[i]);
            }

            output.Add(new SharedAdminLog(
                scanner.Id,
                scanner.Type,
                scanner.Impact,
                AdtLogChunkFormat.FromUnixMs(scanner.DateMs),
                Encoding.UTF8.GetString(scanner.Message),
                players));

            if (output.Count >= take)
                return;
        }
    }

    private bool Matches(AdtLogChunkHeader header, ref AdtLogChunkScanner scanner, char[] charBuffer, LogFilter filter)
    {
        if (filter.Types != null && filter.Types.Count != _logTypeCount && !filter.Types.Contains(scanner.Type))
            return false;

        if (filter.Impacts != null && !filter.Impacts.Contains(scanner.Impact))
            return false;

        if (filter.Before != null || filter.After != null)
        {
            var date = AdtLogChunkFormat.FromUnixMs(scanner.DateMs);

            if (filter.Before != null && date >= filter.Before)
                return false;

            if (filter.After != null && date <= filter.After)
                return false;
        }

        if (filter.LastLogId != null)
        {
            if (filter.DateOrder == DateOrder.Descending && scanner.Id >= filter.LastLogId)
                return false;

            if (filter.DateOrder == DateOrder.Ascending && scanner.Id <= filter.LastLogId)
                return false;
        }

        if (!MatchesPlayers(header, ref scanner, filter))
            return false;

        return filter.Search == null || MatchesSearch(ref scanner, charBuffer, filter.Search);
    }

    private bool MatchesPlayers(AdtLogChunkHeader header, ref AdtLogChunkScanner scanner, LogFilter filter)
    {
        if (!filter.IncludePlayers)
            return scanner.PlayerCount == 0;

        if (scanner.PlayerCount == 0)
        {
            if (filter.AnyPlayers == null && filter.AllPlayers == null)
                return true;

            return filter.IncludeNonPlayers;
        }

        if (filter.AnyPlayers != null)
        {
            var found = false;

            foreach (var index in scanner.Players)
            {
                if (Array.IndexOf(filter.AnyPlayers, PlayerAt(header, index)) < 0)
                    continue;

                found = true;
                break;
            }

            if (!found)
                return false;
        }

        if (filter.AllPlayers != null)
        {
            foreach (var wanted in filter.AllPlayers)
            {
                var found = false;

                foreach (var index in scanner.Players)
                {
                    if (PlayerAt(header, index) != wanted)
                        continue;

                    found = true;
                    break;
                }

                if (!found)
                    return false;
            }
        }

        return true;
    }

    private static Guid PlayerAt(AdtLogChunkHeader header, int index)
    {
        if (index < 0 || index >= header.Players.Length)
            return Guid.Empty;

        return header.Players[index];
    }

    private bool MatchesSearch(ref AdtLogChunkScanner scanner, char[] charBuffer, string search)
    {
        var message = scanner.Message;

        if (charBuffer.Length < message.Length)
            return Encoding.UTF8.GetString(message).Contains(search, StringComparison.OrdinalIgnoreCase);

        var chars = Encoding.UTF8.GetChars(message, charBuffer);

        return charBuffer.AsSpan(0, chars).Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private void TakeFromTail(
        List<SharedAdminLog> tail,
        LogFilter filter,
        List<SharedAdminLog> results,
        int limit,
        bool descending)
    {
        for (var i = 0; i < tail.Count; i++)
        {
            var log = descending ? tail[tail.Count - 1 - i] : tail[i];

            if (!MatchesTail(log, filter))
                continue;

            results.Add(log);

            if (results.Count >= limit)
                return;
        }
    }

    private bool MatchesTail(SharedAdminLog log, LogFilter filter)
    {
        if (filter.Types != null && filter.Types.Count != _logTypeCount && !filter.Types.Contains(log.Type))
            return false;

        if (filter.Impacts != null && !filter.Impacts.Contains(log.Impact))
            return false;

        if (filter.Before != null && log.Date >= filter.Before)
            return false;

        if (filter.After != null && log.Date <= filter.After)
            return false;

        if (filter.LastLogId != null)
        {
            if (filter.DateOrder == DateOrder.Descending && log.Id >= filter.LastLogId)
                return false;

            if (filter.DateOrder == DateOrder.Ascending && log.Id <= filter.LastLogId)
                return false;
        }

        if (!filter.IncludePlayers)
            return log.Players.Length == 0;

        if (log.Players.Length == 0)
        {
            if (filter.AnyPlayers != null || filter.AllPlayers != null)
                return filter.IncludeNonPlayers;
        }
        else
        {
            if (filter.AnyPlayers != null && !filter.AnyPlayers.Any(player => log.Players.Contains(player)))
                return false;

            if (filter.AllPlayers != null && !filter.AllPlayers.All(player => log.Players.Contains(player)))
                return false;
        }

        return filter.Search == null || log.Message.Contains(filter.Search, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Кэш

    private RoundCache GetOrCreateRound(int roundId, bool ours)
    {
        if (!_rounds.TryGetValue(roundId, out var round))
        {
            round = new RoundCache { RoundId = roundId };

            if (ours)
            {
                round.HeadersLoaded = true;
                round.NextChunkIndex = roundId == _currentRound ? 0 : -1;
            }

            _rounds.Add(roundId, round);
            _roundOrder.Add(roundId);
        }
        else
        {
            Touch(roundId);
        }

        return round;
    }

    private void Touch(int roundId)
    {
        if (!_roundOrder.Remove(roundId))
            return;

        _roundOrder.Add(roundId);
    }

    private void TrimRounds()
    {
        var keep = Math.Max(1, _cacheRounds);

        while (_roundOrder.Count > keep)
        {
            var oldest = _roundOrder[0];

            if (oldest == _currentRound)
            {
                if (_roundOrder.Count == 1)
                    return;

                oldest = _roundOrder[1];
                _roundOrder.RemoveAt(1);
            }
            else
            {
                _roundOrder.RemoveAt(0);
            }

            if (_rounds.Remove(oldest, out var round))
                _cachedBytes -= round.PayloadBytes();
        }
    }

    private void TrimCache()
    {
        if (_cachedBytes <= _cacheBytes)
            return;

        foreach (var roundId in _roundOrder)
        {
            if (!_rounds.TryGetValue(roundId, out var round))
                continue;

            foreach (var chunk in round.Chunks)
            {
                if (chunk.Payload == null || !chunk.Persisted)
                    continue;

                _cachedBytes -= chunk.Payload.Length;
                chunk.Payload = null;

                if (_cachedBytes <= _cacheBytes)
                    return;
            }
        }
    }

    private sealed class RoundCache
    {
        public int RoundId;
        public readonly List<CachedChunk> Chunks = new();
        public readonly List<SharedAdminLog> Tail = new();
        public bool HeadersLoaded;
        public int NextChunkIndex = -1;

        public int PayloadBytes()
        {
            var bytes = 0;

            foreach (var chunk in Chunks)
            {
                bytes += chunk.Payload?.Length ?? 0;
            }

            return bytes;
        }
    }

    private sealed class CachedChunk
    {
        public AdtLogChunkHeader Header = default!;
        public byte[]? Payload;
        public bool Persisted;
    }

    #endregion
}
