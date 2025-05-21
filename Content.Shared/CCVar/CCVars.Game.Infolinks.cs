﻿using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Link to Discord server to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksDiscord =
        CVarDef.Create("infolinks.discord", "https://discord.gg/9jUHE5bAR7", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to website to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksForum =
        CVarDef.Create("infolinks.forum", "https://community.fandom.com/wiki/User:Space_Dream_-_Space_Station_14?veaction=edit", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to GitHub page to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksGithub =
        CVarDef.Create("infolinks.github", "", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to website to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksWebsite =
        CVarDef.Create("infolinks.website", "https://boosty.to/prichinatryaski/donate", CVar.SERVER | CVar.REPLICATED); // ADT-Tweak

    /// <summary>
    ///     Link to wiki to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksWiki =
        CVarDef.Create("infolinks.wiki", "https://wiki.sd.qeqk.ru", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to Patreon. Not shown in the launcher currently.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksPatreon =
        CVarDef.Create("infolinks.patreon", "", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to the bug report form.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksBugReport =
        CVarDef.Create("infolinks.bug_report", "", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to site handling ban appeals. Shown in ban disconnect messages.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksAppeal =
        CVarDef.Create("infolinks.appeal", "https://discord.com/channels/901772674865455115/1245787985891561544", CVar.SERVER | CVar.REPLICATED); //ADT-Tweak: Ссылка на обжалование

    /// <summary>
    ///     Link to Telegram channel to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksTelegram =
        CVarDef.Create("infolinks.telegram", "", CVar.SERVER | CVar.REPLICATED);
}
