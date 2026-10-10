using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Shared.ADT.InconnuOS;
using Content.Shared.ADT.InconnuOS.NanoNet;
using Content.Shared.ADT.Sponsors;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.InconnuOS.NanoNet;

public readonly record struct NanoNetResponse(string Url, string DisplayUrl, bool Found, string Html)
{
    public static readonly NanoNetResponse Invalid = new(string.Empty, string.Empty, false, string.Empty);
}

public sealed class NanoNetSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ISharedSponsorManager _sponsors = default!;
    [Dependency] private readonly ITaskManager _task = default!;

    private static readonly ProtoId<NanoNetSitePrototype> HomeSite = "NanoNetHome";

    private const string DisplayScheme = "ntnet";

    private const string RealScheme = "http";

    private const string Tld = ".nt";

    private static readonly TimeSpan StorageRetryDelay = TimeSpan.FromSeconds(30);

    private const string DefaultStyle = """
        body { background:#0d1117; color:#c9d1d9; font-family:sans-serif; margin:0; padding:24px; }
        a { color:#58a6ff; }
        h1 { border-bottom:1px solid #30363d; padding-bottom:8px; }
        """;

    private readonly Dictionary<string, NanoNetSite> _sites = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, NanoNetSitePrototype> _builtIn = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<NetUserId, TimeSpan> _nextPublish = new();
    private readonly Dictionary<string, NanoNetStoredSite?> _writes = new(StringComparer.OrdinalIgnoreCase);

    private NanoNetAutomod _automod = default!;

    private Task _loadTask = Task.CompletedTask;
    private Task _writeTask = Task.CompletedTask;
    private bool _loaded;
    private TimeSpan _nextStorageRetry;

    private sealed record NanoNetSite(
        NetUserId OwnerId,
        string OwnerName,
        string Html,
        DateTime PublishedAt,
        bool Persistent);

    public readonly record struct NanoNetSiteInfo(string Label, string OwnerName, NetUserId Owner, bool Persistent);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        RebuildBuiltIn();
        RebuildAutomod();

        _loadTask = LoadStored();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.RealTime < _nextStorageRetry)
            return;

        if (!_loaded && _loadTask.IsCompleted)
            _loadTask = LoadStored();

        if (_writes.Count > 0 && _writeTask.IsCompleted)
            _writeTask = Flush();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        foreach (var label in _sites.Where(e => !e.Value.Persistent).Select(e => e.Key).ToList())
        {
            _sites.Remove(label);
        }

        _nextPublish.Clear();
    }

    private async Task LoadStored()
    {
        List<NanoNetStoredSite> stored;

        try
        {
            stored = await _db.GetNanoNetSitesAsync();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load NanoNet sites, publishing is disabled until it succeeds: {e}");
            _nextStorageRetry = _timing.RealTime + StorageRetryDelay;
            return;
        }

        foreach (var site in stored)
        {
            if (_writes.ContainsKey(site.Label))
                continue;

            _sites.TryAdd(site.Label,
                new NanoNetSite(new NetUserId(site.UserId), site.OwnerName, site.Html, site.PublishedAt, true));
        }

        _loaded = true;
    }

    private void QueueWrite(string label, NanoNetStoredSite? site)
    {
        _writes[label] = site;

        if (_writeTask.IsCompleted)
            _writeTask = Flush();
    }

    private async Task Flush()
    {
        var batch = new Dictionary<string, NanoNetStoredSite?>(_writes, StringComparer.OrdinalIgnoreCase);

        var upsert = batch.Values.OfType<NanoNetStoredSite>().ToList();
        var delete = batch.Where(e => e.Value == null).Select(e => e.Key).ToList();

        try
        {
            await _db.SyncNanoNetSitesAsync(upsert, delete);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save {batch.Count} NanoNet site changes, retrying later: {e}");
            _nextStorageRetry = _timing.RealTime + StorageRetryDelay;
            return;
        }

        foreach (var (label, written) in batch)
        {
            if (_writes.TryGetValue(label, out var current) && ReferenceEquals(current, written))
                _writes.Remove(label);
        }
    }

    public void FlushForShutdown()
    {
        for (var attempt = 0; attempt < 3 && (_writes.Count > 0 || !_writeTask.IsCompleted); attempt++)
        {
            if (_writeTask.IsCompleted)
                _writeTask = Flush();

            _task.BlockWaitOnTask(_writeTask);
        }

        if (_writes.Count > 0)
            Log.Error($"{_writes.Count} NanoNet site changes were lost on shutdown");
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<NanoNetSitePrototype>())
            RebuildBuiltIn();

        if (args.WasModified<NanoNetFilterPrototype>())
            RebuildAutomod();
    }

    private void RebuildAutomod()
    {
        _automod = new NanoNetAutomod(_proto.EnumeratePrototypes<NanoNetFilterPrototype>(), Log);
    }

    private void RebuildBuiltIn()
    {
        _builtIn.Clear();

        foreach (var site in _proto.EnumeratePrototypes<NanoNetSitePrototype>())
        {
            _builtIn[site.Host] = site;
        }
    }

    public NanoNetResponse Fetch(string input)
    {
        if (input.Length > _cfg.GetCVar(NanoNetCVars.MaxUrlLength))
            return NanoNetResponse.Invalid;

        if (string.IsNullOrWhiteSpace(input))
            input = _proto.Index(HomeSite).Host;

        if (!TryParse(input, out var host, out var path))
            return NanoNetResponse.Invalid;

        var url = $"{RealScheme}://{host}{path}";

        if (_builtIn.TryGetValue(host, out var builtIn))
        {
            var display = $"{builtIn.Scheme ?? DisplayScheme}://{host}{path}";

            return builtIn.Pages.TryGetValue(path, out var page)
                ? new NanoNetResponse(url, display, true, RenderPage(builtIn, page, path))
                : new NanoNetResponse(url, display, false, RenderPageNotFound(builtIn, path));
        }

        var displayUrl = $"{DisplayScheme}://{host}{path}";

        if (host.EndsWith(Tld, StringComparison.Ordinal) && _sites.TryGetValue(host[..^Tld.Length], out var site))
            return new NanoNetResponse(url, displayUrl, true, NanoNetWidgets.Apply(site.Html));

        return new NanoNetResponse(url, displayUrl, false, RenderSiteNotFound(host));
    }

    public bool TryPublish(
        EntityUid actor,
        ICommonSession session,
        string ownerName,
        string domain,
        string html,
        out string label,
        out TimeSpan publishedAt,
        out OsValidationError error,
        out string detail)
    {
        publishedAt = TimeSpan.Zero;
        detail = domain;

        if (!TryNormalizeLabel(domain, out label, out error))
            return false;

        if (!_loaded)
        {
            error = OsValidationError.NanoNetUnavailable;
            return false;
        }

        var maxSiteLength = _cfg.GetCVar(NanoNetCVars.MaxSiteLength);

        if (html.Length > maxSiteLength)
        {
            error = OsValidationError.SiteTooLarge;
            detail = maxSiteLength.ToString();
            return false;
        }

        var owner = session.UserId;
        var now = _timing.CurTime;

        if (_nextPublish.TryGetValue(owner, out var next) && now < next)
        {
            error = OsValidationError.PublishCooldown;
            detail = Math.Ceiling((next - now).TotalSeconds).ToString();
            return false;
        }

        _nextPublish[owner] = now + TimeSpan.FromSeconds(_cfg.GetCVar(NanoNetCVars.PublishCooldown));

        const bool allowScripts = false;

        var verdict = _automod.CheckLabel(label);
        if (!verdict.Rejected)
            verdict = _automod.CheckSite(html, allowScripts);

        if (verdict.Rejected)
        {
            error = verdict.Error;
            detail = verdict.Detail;

            _adminLog.Add(LogType.NanoNet, LogImpact.Medium,
                $"{ToPrettyString(actor):player} was blocked from publishing NanoNet site {label}: {error} ({verdict.Match})");
            return false;
        }

        if (_sites.TryGetValue(label, out var existing) && existing.OwnerId != owner)
        {
            error = OsValidationError.DomainNotOwned;
            detail = label;
            return false;
        }

        if (existing == null)
        {
            var maxSitesTotal = _cfg.GetCVar(NanoNetCVars.MaxSitesTotal);

            if (_sites.Count >= maxSitesTotal)
            {
                error = OsValidationError.TooManySites;
                detail = maxSitesTotal.ToString();
                return false;
            }

            var maxSitesPerOwner = _cfg.GetCVar(NanoNetCVars.MaxSitesPerOwner);

            if (_sites.Values.Count(s => s.OwnerId == owner) >= maxSitesPerOwner)
            {
                error = OsValidationError.TooManySites;
                detail = maxSitesPerOwner.ToString();
                return false;
            }
        }

        var persistent = _sponsors.TryGetData(session, out var sponsor) && sponsor.NanoNetPersistSites;
        var site = new NanoNetSite(owner, ownerName, html, DateTime.UtcNow, persistent);

        publishedAt = now;
        _sites[label] = site;

        if (persistent)
            QueueWrite(label, new NanoNetStoredSite(label, owner.UserId, ownerName, html, site.PublishedAt));
        else if (existing is { Persistent: true })
            QueueWrite(label, null);

        _adminLog.Add(LogType.NanoNet, LogImpact.Low,
            $"{ToPrettyString(actor):player} published NanoNet site {label} ({html.Length} chars, persistent: {persistent})");
        return true;
    }

    public bool TryUnpublish(EntityUid actor, NetUserId owner, string domain, out string label, out OsValidationError error, out string detail)
    {
        detail = domain;

        if (!TryNormalizeLabel(domain, out label, out error))
            return false;

        detail = label;

        if (!_loaded)
        {
            error = OsValidationError.NanoNetUnavailable;
            return false;
        }

        if (!_sites.TryGetValue(label, out var existing))
        {
            error = OsValidationError.DomainNotFound;
            return false;
        }

        if (existing.OwnerId != owner)
        {
            error = OsValidationError.DomainNotOwned;
            return false;
        }

        _sites.Remove(label);

        if (existing.Persistent)
            QueueWrite(label, null);

        _adminLog.Add(LogType.NanoNet, LogImpact.Low, $"{ToPrettyString(actor):player} unpublished NanoNet site {label}");
        return true;
    }

    public IEnumerable<NanoNetSiteInfo> GetSites()
    {
        return _sites
            .OrderByDescending(e => e.Value.PublishedAt)
            .Select(e => new NanoNetSiteInfo(e.Key, e.Value.OwnerName, e.Value.OwnerId, e.Value.Persistent));
    }

    public bool TryForceUnpublish(string domain, string admin)
    {
        var label = domain.Trim().ToLowerInvariant();

        if (label.EndsWith(Tld, StringComparison.Ordinal))
            label = label[..^Tld.Length];

        var found = _sites.Remove(label, out var site);

        if (!found && _loaded)
            return false;

        if (site == null || site.Persistent)
            QueueWrite(label, null);

        var owner = site == null ? "unknown (not loaded yet)" : $"{site.OwnerName} ({site.OwnerId})";
        _adminLog.Add(LogType.NanoNet, LogImpact.Medium,
            $"{admin} force-unpublished NanoNet site {label} owned by {owner}");
        return true;
    }

    private bool TryNormalizeLabel(string domain, out string label, out OsValidationError error)
    {
        if (!NanoNetDomain.TryNormalizeLabel(domain, out label, out error))
            return false;

        if (_builtIn.ContainsKey(NanoNetDomain.GetHost(label)))
        {
            error = OsValidationError.DomainReserved;
            return false;
        }

        return true;
    }

    private static bool TryParse(string input, out string host, out string path)
    {
        var rest = input.Trim();

        var schemeIndex = rest.IndexOf("://", StringComparison.Ordinal);
        if (schemeIndex >= 0)
            rest = rest[(schemeIndex + 3)..];

        var cutIndex = rest.IndexOfAny(new[] { '?', '#' });
        if (cutIndex >= 0)
            rest = rest[..cutIndex];

        var pathIndex = rest.IndexOf('/');
        host = (pathIndex < 0 ? rest : rest[..pathIndex]).ToLowerInvariant();
        path = pathIndex < 0 ? "/" : rest[pathIndex..].ToLowerInvariant();

        var portIndex = host.IndexOf(':');
        if (portIndex >= 0)
            host = host[..portIndex];

        path = path.TrimEnd('/');
        if (path.Length == 0)
            path = "/";

        return IsValidHost(host) && IsValidPath(path);
    }

    private static bool IsValidHost(string host)
    {
        if (host.Length == 0 || host.Length > 253)
            return false;

        if (host[0] == '.' || host[^1] == '.' || host.Contains(".."))
            return false;

        foreach (var c in host)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '-' && c != '.')
                return false;
        }

        return true;
    }

    private static bool IsValidPath(string path)
    {
        if (path.Contains("//") || path.Contains("/."))
            return false;

        foreach (var c in path)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '/' && c != '-' && c != '_' && c != '.')
                return false;
        }

        return true;
    }

    private static string ToRealUrl(string url)
    {
        var schemeIndex = url.IndexOf("://", StringComparison.Ordinal);
        return schemeIndex < 0 ? url : $"{RealScheme}{url[schemeIndex..]}";
    }

    private string RenderPage(NanoNetSitePrototype site, NanoNetPage page, string path)
    {
        var title = HtmlEncode(Loc.GetString(page.Title));

        var body = new StringBuilder();
        body.Append($"<h1>{title}</h1><p>{HtmlEncode(Loc.GetString(page.Body))}</p>");

        if (page.Links.Count > 0)
        {
            body.Append("<ul>");

            foreach (var link in page.Links)
            {
                body.Append($"<li><a href=\"{HtmlEncode(ToRealUrl(link.Url))}\">{HtmlEncode(Loc.GetString(link.Text))}</a></li>");
            }

            body.Append("</ul>");
        }

        if (path == "/" && site.ID == HomeSite.Id)
            body.Append(RenderDirectory());

        return RenderDocument(title, body.ToString(), site.Style);
    }

    private string RenderDirectory()
    {
        var title = HtmlEncode(Loc.GetString("nanonet-directory-title"));

        if (_sites.Count == 0)
            return $"<h2>{title}</h2><p>{HtmlEncode(Loc.GetString("nanonet-directory-empty"))}</p>";

        var body = new StringBuilder();
        body.Append($"<h2>{title}</h2><ul>");

        foreach (var (label, site) in _sites.OrderByDescending(entry => entry.Value.PublishedAt))
        {
            var host = NanoNetDomain.GetHost(label);
            body.Append(
                $"<li><a href=\"{RealScheme}://{host}/\">{HtmlEncode(host)}</a> — {HtmlEncode(site.OwnerName)}</li>");
        }

        body.Append("</ul>");
        return body.ToString();
    }

    private string RenderPageNotFound(NanoNetSitePrototype site, string path)
    {
        var title = Loc.GetString("nanonet-notfound-title");
        var text = Loc.GetString("nanonet-notfound-page", ("path", HtmlEncode(path)));
        var home = HtmlEncode(Loc.GetString("nanonet-link-home"));

        return RenderDocument(title, $"<h1>{title}</h1><p>{text}</p><p><a href=\"/\">{home}</a></p>", site.Style);
    }

    private string RenderSiteNotFound(string host)
    {
        var title = Loc.GetString("nanonet-notfound-title");
        var text = Loc.GetString("nanonet-notfound-site", ("host", HtmlEncode(host)));

        return RenderDocument(title, $"<h1>{title}</h1><p>{text}</p>");
    }

    private static string RenderDocument(string title, string body, string? style = null)
    {
        return $"""
            <!doctype html>
            <html>
            <head>
                <meta charset="utf-8">
                <title>{title}</title>
                <style>{style ?? DefaultStyle}</style>
            </head>
            <body>{body}</body>
            </html>
            """;
    }

    private static string HtmlEncode(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
