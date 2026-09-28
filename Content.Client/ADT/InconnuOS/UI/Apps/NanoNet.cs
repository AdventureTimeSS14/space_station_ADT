using System.Net;
using Content.Shared.ADT.InconnuOS.NanoNet;

namespace Content.Client.ADT.InconnuOS.UI.Apps;

public static class NanoNet
{
    private const string AllowedHost = "nanonet.nt";
    private const string Tld = ".nt";

    private static readonly string ErrorTitle;
    private static readonly string ErrorBody;
    private static readonly string HomeTitle;
    private static readonly string HomeWelcome;
    private static readonly string HomeLinkNews;
    private static readonly string HomeLinkAbout;
    private static readonly string NewsTitle;
    private static readonly string NewsBody;
    private static readonly string AboutTitle;
    private static readonly string AboutBody;
    private static readonly string LinkHome;
    private static readonly string NotFoundTitle;
    private static readonly string NotFoundPageBefore;
    private static readonly string NotFoundPageAfter;
    private static readonly string NotFoundSiteBefore;
    private static readonly string NotFoundSiteAfter;

    static NanoNet()
    {
        ErrorTitle = Loc.GetString("nanonet-error-title");
        ErrorBody = Loc.GetString("nanonet-error-body");
        HomeTitle = Loc.GetString("nanonet-home-title");
        HomeWelcome = Loc.GetString("nanonet-home-welcome");
        HomeLinkNews = Loc.GetString("nanonet-home-link-news");
        HomeLinkAbout = Loc.GetString("nanonet-home-link-about");
        NewsTitle = Loc.GetString("nanonet-news-title");
        NewsBody = Loc.GetString("nanonet-news-body");
        AboutTitle = Loc.GetString("nanonet-about-title");
        AboutBody = Loc.GetString("nanonet-about-body");
        LinkHome = Loc.GetString("nanonet-link-home");
        NotFoundTitle = Loc.GetString("nanonet-notfound-title");
        NotFoundPageBefore = Loc.GetString("nanonet-notfound-page-before");
        NotFoundPageAfter = Loc.GetString("nanonet-notfound-page-after");
        NotFoundSiteBefore = Loc.GetString("nanonet-notfound-site-before");
        NotFoundSiteAfter = Loc.GetString("nanonet-notfound-site-after");
    }

    public static void EnsureWarm()
    {
    }

    public static bool IsAllowedHost(string url)
    {
        if (!TryParse(url, out var host, out _))
            return false;

        return host.Length > Tld.Length && host.EndsWith(Tld, StringComparison.OrdinalIgnoreCase);
    }

    public static (HttpStatusCode Status, string Html) Render(string url)
    {
        if (!TryParse(url, out var host, out var path))
            return (HttpStatusCode.BadRequest, Page(ErrorTitle, $"<h1>{ErrorTitle}</h1><p>{ErrorBody}</p>"));

        return (host.ToLowerInvariant(), path.TrimEnd('/')) switch
        {
            (AllowedHost, "") => (HttpStatusCode.OK, Page(HomeTitle, Home())),
            (AllowedHost, "/news") => (HttpStatusCode.OK, Page(NewsTitle, News())),
            (AllowedHost, "/about") => (HttpStatusCode.OK, Page(AboutTitle, About())),
            _ => (HttpStatusCode.NotFound, Page(NotFoundTitle, NotFound(path))),
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
        var host = HtmlEncode(NanoNetDomain.GetHost(label));

        var body = $"""
            <h1>{NotFoundTitle}</h1>
            <p>{NotFoundSiteBefore} {host} {NotFoundSiteAfter}</p>
            """;

        return Page(NotFoundTitle, body);
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
            <h1>{HomeTitle}</h1>
            <p>{HomeWelcome}</p>
            <ul>
                <li><a href="http://nanonet.nt/news">{HomeLinkNews}</a></li>
                <li><a href="http://nanonet.nt/about">{HomeLinkAbout}</a></li>
            </ul>
            """;
    }

    private static string News()
    {
        return $"""
            <h1>{NewsTitle}</h1>
            <p>{NewsBody}</p>
            <p><a href="http://nanonet.nt/">{LinkHome}</a></p>
            """;
    }

    private static string About()
    {
        return $"""
            <h1>{AboutTitle}</h1>
            <p>{AboutBody}</p>
            <p><a href="http://nanonet.nt/">{LinkHome}</a></p>
            """;
    }

    private static string NotFound(string path)
    {
        return $"""
            <h1>{NotFoundTitle}</h1>
            <p>{NotFoundPageBefore} {HtmlEncode(path)} {NotFoundPageAfter}</p>
            <p><a href="http://nanonet.nt/">{LinkHome}</a></p>
            """;
    }
}
