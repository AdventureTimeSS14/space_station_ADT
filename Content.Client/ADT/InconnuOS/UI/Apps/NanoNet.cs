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
            return (HttpStatusCode.BadRequest, Page("Ошибка", "<h1>Некорректный адрес</h1>"));

        return (host, path.TrimEnd('/')) switch
        {
            (AllowedHost, "") => (HttpStatusCode.OK, Page("NanoNet", Home)),
            (AllowedHost, "/news") => (HttpStatusCode.OK, Page("Новости станции", News)),
            (AllowedHost, "/about") => (HttpStatusCode.OK, Page("О сети", About)),
            _ => (HttpStatusCode.NotFound, Page("404", NotFound(path))),
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
        return Page("404", $"""
            <h1>404</h1>
            <p>Сайт {HtmlEncode(NanoNetDomain.GetHost(label))} не опубликован.</p>
            """);
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

    private const string Home = """
        <h1>NanoNet</h1>
        <p>Добро пожаловать во внутреннюю сеть NanoTrasen.</p>
        <ul>
            <li><a href="http://nanonet.nt/news">Новости</a></li>
            <li><a href="http://nanonet.nt/about">О сети</a></li>
        </ul>
        """;

    private const string News = """
        <h1>Новости станции</h1>
        <p>Сегодня в столовой закончился кофе. Персонал в панике.</p>
        <p><a href="http://nanonet.nt/">На главную</a></p>
        """;

    private const string About = """
        <h1>О сети</h1>
        <p>NanoNet - внутренняя сеть NanoTrasen.</p>
        <p><a href="http://nanonet.nt/">На главную</a></p>
        """;

    private static string NotFound(string path)
    {
        return $"""
            <h1>404</h1>
            <p>Страница {HtmlEncode(path)} не найдена.</p>
            <p><a href="http://nanonet.nt/">На главную</a></p>
            """;
    }
}
