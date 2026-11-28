using Robust.Shared.Configuration;

namespace Content.Shared.ADT.CCVar;

public sealed partial class ADTCCVars
{
    public static readonly CVarDef<bool> CanvasEnabled =
        CVarDef.Create("adt.canvas.enabled", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> CanvasWhitelist =
        CVarDef.Create("adt.canvas.whitelist", true, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasRulesAccepted =
        CVarDef.Create("adt.canvas.rules_accepted", 0, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> CanvasMaxPaintingMb =
        CVarDef.Create("adt.canvas.max_painting_mb", 24, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasMaxLayersMb =
        CVarDef.Create("adt.canvas.max_layers_mb", 48, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasMaxSessions =
        CVarDef.Create("adt.canvas.max_sessions", 12, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasMaxSessionsPerPlayer =
        CVarDef.Create("adt.canvas.max_sessions_per_player", 1, CVar.SERVERONLY);

    public static readonly CVarDef<float> CanvasHostGrace =
        CVarDef.Create("adt.canvas.host_grace", 300f, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasMaxEditors =
        CVarDef.Create("adt.canvas.max_editors", 4, CVar.SERVERONLY);

    public static readonly CVarDef<float> CanvasJoinRequestCooldown =
        CVarDef.Create("adt.canvas.join_request_cooldown", 60f, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasStrokeSamplesPerSec =
        CVarDef.Create("adt.canvas.stroke_samples_per_sec", 1000, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasCommitDebounceMs =
        CVarDef.Create("adt.canvas.commit_debounce_ms", 600, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> CanvasAutosaveInterval =
        CVarDef.Create("adt.canvas.autosave_interval", 120f, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasDownKbps =
        CVarDef.Create("adt.canvas.down_kbps", 512, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasUpKbps =
        CVarDef.Create("adt.canvas.up_kbps", 256, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> CanvasDraftSlots =
        CVarDef.Create("adt.canvas.draft_slots", 2, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasDraftTtlDays =
        CVarDef.Create("adt.canvas.draft_ttl_days", 14, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasPendingPerPlayer =
        CVarDef.Create("adt.canvas.pending_per_player", 2, CVar.SERVERONLY);

    public static readonly CVarDef<float> CanvasSubmitCooldown =
        CVarDef.Create("adt.canvas.submit_cooldown", 600f, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasRejectTtlDays =
        CVarDef.Create("adt.canvas.reject_ttl_days", 3, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasApprovedPerPlayer =
        CVarDef.Create("adt.canvas.approved_per_player", 20, CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasApprovedTotalMb =
        CVarDef.Create("adt.canvas.approved_total_mb", 0, CVar.SERVERONLY);

    public static readonly CVarDef<string> CanvasBlobStore =
        CVarDef.Create("adt.canvas.blob_store", "db", CVar.SERVERONLY);

    public static readonly CVarDef<string> CanvasBlobDir =
        CVarDef.Create("adt.canvas.blob_dir", "", CVar.SERVERONLY);

    public static readonly CVarDef<int> CanvasClientCacheMb =
        CVarDef.Create("adt.canvas.client_cache_mb", 512, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> CanvasPressurePreset =
        CVarDef.Create("adt.canvas.pressure_preset", "Liner", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CanvasRightClickPicker =
        CVarDef.Create("adt.canvas.right_click_picker", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
