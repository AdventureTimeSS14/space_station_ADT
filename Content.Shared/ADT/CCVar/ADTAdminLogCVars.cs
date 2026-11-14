using Robust.Shared.Configuration;

namespace Content.Shared.ADT.CCVar;

[CVarDefs]
public sealed class ADTAdminLogCVars
{
    public static readonly CVarDef<bool> AdminLogsChunksEnabled =
        CVarDef.Create("adt_adminlogs.chunks_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Дублировать ли логи в старую таблицу admin_log
    /// </summary>
    public static readonly CVarDef<bool> AdminLogsWriteLegacy =
        CVarDef.Create("adt_adminlogs.write_legacy", false, CVar.SERVERONLY);

    /// <summary>
    /// Максимум логов в одном чанке.
    /// </summary>
    public static readonly CVarDef<int> AdminLogsChunkMaxLogs =
        CVarDef.Create("adt_adminlogs.chunk_max_logs", 4096, CVar.SERVERONLY);

    /// <summary>
    /// Максимальный размер несжатого чанка в байтах.
    /// </summary>
    public static readonly CVarDef<int> AdminLogsChunkMaxBytes =
        CVarDef.Create("adt_adminlogs.chunk_max_bytes", 1048576, CVar.SERVERONLY);

    /// <summary>
    /// Уровень сжатия zstd.
    /// </summary>
    public static readonly CVarDef<int> AdminLogsChunkCompression =
        CVarDef.Create("adt_adminlogs.chunk_compression", 6, CVar.SERVERONLY);

    /// <summary>
    /// Сколько последних раундов держать сжатыми в оперативке.
    /// </summary>
    public static readonly CVarDef<int> AdminLogsCacheRounds =
        CVarDef.Create("adt_adminlogs.cache_rounds", 3, CVar.SERVERONLY);

    /// <summary>
    /// Жёсткий потолок памяти под кэш сжатых чанков в байтах.
    /// </summary>
    public static readonly CVarDef<int> AdminLogsCacheBytes =
        CVarDef.Create("adt_adminlogs.cache_bytes", 33554432, CVar.SERVERONLY);

    /// <summary>
    /// Сколько чанков тянуть из базы за один запрос.
    /// </summary>
    public static readonly CVarDef<int> AdminLogsFetchBatch =
        CVarDef.Create("adt_adminlogs.fetch_batch", 16, CVar.SERVERONLY);
}
