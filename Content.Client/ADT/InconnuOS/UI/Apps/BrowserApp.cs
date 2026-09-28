using System.IO;
using System.Net;
using System.Text;
using Content.Shared.ADT.InconnuOS.NanoNet;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.WebView;

namespace Content.Client.ADT.InconnuOS.UI.Apps;

public sealed class BrowserApp : OsAppControl
{
    private const string HomeUrl = "http://nanonet.nt/";

    private readonly WebViewControl _web;
    private readonly LineEdit _address;
    private readonly Button _back;
    private readonly Button _forward;
    private readonly Button _reload;
    private readonly Button _home;
    private readonly Label _status;

    private bool _subscribed;

    private int? _pendingRequestId;
    private string? _pendingUrl;

    private (int RequestId, string Label, string Path, string Html, HttpStatusCode Status)? _answer;

    public BrowserApp()
    {
        NanoNet.EnsureWarm();

        _back = OsWidgets.Small("<");
        _forward = OsWidgets.Small(">");
        _reload = OsWidgets.Small(Loc.GetString("os-browser-reload"));
        _home = OsWidgets.Small(Loc.GetString("os-browser-home"));

        _address = new LineEdit
        {
            HorizontalExpand = true,
            Margin = new Thickness(4f, 0f, 4f, 0f),
        };

        var toolbar = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(4f, 4f, 4f, 2f),
            Children = { _back, _forward, _reload, _home, _address },
        };

        _status = new Label
        {
            Modulate = OsStyle.TextDim,
            Margin = new Thickness(4f, 0f, 4f, 2f),
            Visible = false,
        };

        _web = new WebViewControl
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        _web.AddBeforeBrowseHandler(OnBeforeBrowse);
        _web.AddResourceRequestHandler(OnResourceRequest);

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            Children = { toolbar, _status, _web },
        });

        _back.OnPressed += _ => _web.GoBack();
        _forward.OnPressed += _ => _web.GoForward();
        _reload.OnPressed += _ => _web.Reload();
        _home.OnPressed += _ => Navigate(HomeUrl);
        _address.OnTextEntered += args => Navigate(NormalizeUrl(args.Text));
    }

    public override void OnOpen(string? argument)
    {
        base.OnOpen(argument);

        if (!_subscribed)
        {
            _subscribed = true;
            Context.MessageReceived += OnServerMessage;
        }

        SetTitle(Loc.GetString("os-app-browser"));

        Navigate(argument is { Length: > 0 } ? NormalizeUrl(argument) : HomeUrl);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || !_subscribed)
            return;

        Context.MessageReceived -= OnServerMessage;
    }

    private void OnBeforeBrowse(IBeforeBrowseContext ctx)
    {
        if (!NanoNet.IsAllowedHost(ctx.Url))
        {
            ctx.DoCancel();
            return;
        }

        if (!NanoNet.TryParse(ctx.Url, out var host, out var path))
            return;

        var label = NanoNet.GetLabel(host);

        if (label.Length == 0)
            return;

        if (_answer is { } answer && answer.Label == label && answer.Path == path)
            return;

        ctx.DoCancel();
        BeginFetch(label, path, ctx.Url);
    }

    private void OnResourceRequest(IRequestHandlerContext ctx)
    {
        if (!NanoNet.IsAllowedHost(ctx.Url))
        {
            ctx.DoCancel();
            return;
        }

        if (!NanoNet.TryParse(ctx.Url, out var host, out var path))
        {
            ctx.DoCancel();
            return;
        }

        var label = NanoNet.GetLabel(host);

        if (label.Length == 0)
        {
            var (status, html) = NanoNet.Render(ctx.Url);
            Respond(ctx, status, html);
            return;
        }

        if (_answer is { } answer && answer.Label == label && answer.Path == path)
        {
            Respond(ctx, answer.Status, answer.Html);
            _answer = null;
            return;
        }

        ctx.DoCancel();
    }

    private void Respond(IRequestHandlerContext ctx, HttpStatusCode status, string html)
    {
        var bytes = Encoding.UTF8.GetBytes(html);

        ctx.DoRespondStream(new MemoryStream(bytes), "text/html", status);

        if (ctx.IsNavigation)
            _address.Text = ctx.Url;
    }

    private void BeginFetch(string label, string path, string url)
    {
        var id = Context.NextNanoNetRequestId();

        _pendingRequestId = id;
        _pendingUrl = url;

        SetLoading(true, url);

        Context.Send(new ADTOsNanoNetFetchRequestMessage(id, label, path));
    }

    private void OnServerMessage(BoundUserInterfaceMessage message)
    {
        if (message is not ADTOsNanoNetFetchResponseMessage resp)
            return;

        if (_pendingRequestId != resp.RequestId)
            return;

        _pendingRequestId = null;
        var url = _pendingUrl;
        _pendingUrl = null;

        SetLoading(false, null);

        if (url == null)
            return;

        var html = resp.Found ? resp.Html : NanoNet.PlayerSiteNotFound(resp.Domain);
        var status = resp.Found ? HttpStatusCode.OK : HttpStatusCode.NotFound;

        _answer = (resp.RequestId, resp.Domain, resp.Path, html, status);

        _web.Url = $"{url}#_ln={resp.RequestId}";
    }

    private void SetLoading(bool loading, string? url)
    {
        _back.Disabled = loading;
        _forward.Disabled = loading;
        _reload.Disabled = loading;
        _home.Disabled = loading;

        _status.Visible = loading;

        if (loading && url != null)
            _status.Text = Loc.GetString("os-browser-loading", ("url", url));
    }

    private void Navigate(string url)
    {
        _address.Text = url;
        _web.Url = url;
    }

    private static string NormalizeUrl(string text)
    {
        text = text.Trim();

        if (text.Length == 0)
            return HomeUrl;

        if (!text.Contains("://"))
            text = "http://" + text;

        return text;
    }
}
