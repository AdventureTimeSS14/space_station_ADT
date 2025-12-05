using Robust.Shared.Configuration;
using Robust.Shared;

namespace Content.Shared.ADT.CCVar;

[CVarDefs]
public sealed class DiscordAuthCCVars : CVars
{
    /// <summary>
    /// Enables or disables authorization via Discord.
    /// </summary>
    public static readonly CVarDef<bool> DiscordAuthEnable =
        CVarDef.Create("discord.auth_enable", false, CVar.SERVERONLY | CVar.ARCHIVE);
}
