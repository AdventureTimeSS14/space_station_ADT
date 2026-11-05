using Robust.Shared.Configuration;

namespace Content.Shared.ADT.Polls;

[CVarDefs]
public sealed class PollCVars
{
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("adt.polls.enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<int> TextMaxLength =
        CVarDef.Create("adt.polls.text_max_length", 1000, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> RetentionDays =
        CVarDef.Create("adt.polls.retention_days", 180, CVar.SERVERONLY);
}
