using System.Net;
using Content.Shared.ADT.InconnuOS.NanoNet;

namespace Content.Client.ADT.InconnuOS.UI.Apps;

public static class NanoNet
{
    private const string AllowedHost = "nanonet.nt";
    private const string Tld = ".nt";

    public static bool IsAllowedHost(string url)
    {
        if (!TryParse(url, out var host, out _))
            return false;

        return host.Length > Tld.Length && host.EndsWith(Tld, StringComparison.OrdinalIgnoreCase);
    }

    public static (HttpStatusCode Status, string Html) Render(string url)
    {
        if (!TryParse(url, out var host, out var path))
            return (HttpStatusCode.BadRequest, Page(Loc.GetString("nanonet-error-title"), $"<h1>{Loc.GetString("nanonet-error-title")}</h1><p>{Loc.GetString("nanonet-error-body")}</p>"));

        return (host.ToLowerInvariant(), path.TrimEnd('/')) switch
        {
            (AllowedHost, "") => (HttpStatusCode.OK, Page(Loc.GetString("nanonet-home-title"), Home())),
            (AllowedHost, "/news") => (HttpStatusCode.OK, Page(Loc.GetString("nanonet-news-title"), News())),
            (AllowedHost, "/about") => (HttpStatusCode.OK, Page(Loc.GetString("nanonet-about-title"), About())),
            _ => (HttpStatusCode.NotFound, Page(Loc.GetString("nanonet-notfound-title"), NotFound(path))),
        };
    }

    public static string GetLabel(string host)
    {
        if (host.Equals(AllowedHost, StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        if (!host.EndsWith(Tld, StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        return host[..^Tld.Length];
    }

    public static string PlayerSiteNotFound(string label)
    {
        var body = $"""
            <h1>{Loc.GetString("nanonet-notfound-title")}</h1>
            <p>{Loc.GetString("nanonet-notfound-site", ("host", HtmlEncode(NanoNetDomain.GetHost(label))))}</p>
            """;

        return Page(Loc.GetString("nanonet-notfound-title"), body);
    }

    internal static bool TryParse(string url, out string host, out string path)
    {
        host = string.Empty;
        path = "/";

        var schemeIndex = url.IndexOf("://", StringComparison.Ordinal);
        if (schemeIndex < 0)
            return false;

        var rest = url[(schemeIndex + 3)..];
        var pathIndex = rest.IndexOf('/');
        var hostPart = pathIndex < 0 ? rest : rest[..pathIndex];

        path = pathIndex < 0 ? "/" : rest[pathIndex..];

        var cutIndex = path.IndexOfAny(new[] { '?', '#' });
        if (cutIndex >= 0)
            path = path[..cutIndex];

        var portIndex = hostPart.IndexOf(':');
        host = portIndex < 0 ? hostPart : hostPart[..portIndex];

        return host.Length > 0;
    }

    private static string HtmlEncode(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    private const string Style = """
        body { background:#0d1117; color:#c9d1d9; font-family:sans-serif; margin:0; padding:24px; }
        a { color:#58a6ff; }
        h1 { border-bottom:1px solid #30363d; padding-bottom:8px; }
        """;

    private static string Page(string title, string body)
    {
        return $"""
            <!doctype html>
            <html>
            <head>
                <meta charset="utf-8">
                <title>{title}</title>
                <style>{Style}</style>
            </head>
            <body>{body}</body>
            </html>
            """;
    }

    private static string Home()
    {
        return $"""
            <h1>{Loc.GetString("nanonet-home-title")}</h1>
            <p>{Loc.GetString("nanonet-home-welcome")}</p>
            <ul>
                <li><a href="http://nanonet.nt/news">{Loc.GetString("nanonet-home-link-news")}</a></li>
                <li><a href="http://nanonet.nt/about">{Loc.GetString("nanonet-home-link-about")}</a></li>
            </ul>
            """;
    }

    private static string News()
    {
        return $"""
            <h1>{Loc.GetString("nanonet-news-title")}</h1>
            <p>{Loc.GetString("nanonet-news-body")}</p>
            <p><a href="http://nanonet.nt/">{Loc.GetString("nanonet-link-home")}</a></p>
            """;
    }

    private static string About()
    {
        return $"""
            <h1>{Loc.GetString("nanonet-about-title")}</h1>
            <p>{Loc.GetString("nanonet-about-body")}</p>
            <p><a href="http://nanonet.nt/">{Loc.GetString("nanonet-link-home")}</a></p>
            """;
    }

    private static string NotFound(string path)
    {
        return $"""
            <h1>{Loc.GetString("nanonet-notfound-title")}</h1>
            <p>{Loc.GetString("nanonet-notfound-page", ("path", HtmlEncode(path)))}</p>
            <p><a href="http://nanonet.nt/">{Loc.GetString("nanonet-link-home")}</a></p>
            """;
    }
}
