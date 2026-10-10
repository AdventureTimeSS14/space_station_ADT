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
    private readonly OsWebView _view;
    private readonly WebViewControl _web;
    private readonly LineEdit _address;
    private readonly Button _back;
    private readonly Button _forward;
    private readonly Button _reload;
    private readonly Button _home;
    private readonly Label _status;

    private bool _subscribed;

    private readonly List<string> _history = new();
    private int _historyIndex = -1;

    private int? _pendingRequestId;
    private bool _pendingPushesHistory;

    private ADTOsNanoNetFetchResponseMessage? _page;

    public BrowserApp()
    {
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

        _view = new OsWebView
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        _web = _view.Web;

        _web.AddBeforeBrowseHandler(OnBeforeBrowse);
        _web.AddResourceRequestHandler(OnResourceRequest);

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            Children = { toolbar, _status, _view },
        });

        _back.OnPressed += _ => StepHistory(-1);
        _forward.OnPressed += _ => StepHistory(1);
        _reload.OnPressed += _ => Reload();
        _home.OnPressed += _ => Fetch(string.Empty, true);
        _address.OnTextEntered += args => Fetch(args.Text, true);

        UpdateButtons();
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

        Fetch(argument ?? string.Empty, true);
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
        if (IsCurrentPage(ctx.Url))
            return;

        ctx.DoCancel();

        if (ctx.UserGesture)
            Fetch(ctx.Url, true);
    }

    private void OnResourceRequest(IRequestHandlerContext ctx)
    {
        if (_page == null || !IsCurrentPage(ctx.Url))
        {
            ctx.DoCancel();
            return;
        }

        var bytes = Encoding.UTF8.GetBytes("﻿" + _page.Html);
        var status = _page.Found ? HttpStatusCode.OK : HttpStatusCode.NotFound;

        ctx.DoRespondStream(new MemoryStream(bytes), "text/html", status);
    }

    private bool IsCurrentPage(string url)
    {
        if (_page == null)
            return false;

        var fragmentIndex = url.IndexOf('#');
        if (fragmentIndex >= 0)
            url = url[..fragmentIndex];

        return string.Equals(url, _page.Url, StringComparison.OrdinalIgnoreCase);
    }

    private void StepHistory(int delta)
    {
        var index = _historyIndex + delta;
        if (index < 0 || index >= _history.Count)
            return;

        _historyIndex = index;
        Fetch(_history[index], false);
    }

    private void Reload()
    {
        if (_historyIndex >= 0)
            Fetch(_history[_historyIndex], false);
    }

    private void Fetch(string url, bool pushHistory)
    {
        var id = Context.NextNanoNetRequestId();

        _pendingRequestId = id;
        _pendingPushesHistory = pushHistory;

        SetLoading(true);

        Context.Send(new ADTOsNanoNetFetchRequestMessage(id, url.Trim()));
    }

    private void OnServerMessage(BoundUserInterfaceMessage message)
    {
        if (message is not ADTOsNanoNetFetchResponseMessage resp || _pendingRequestId != resp.RequestId)
            return;

        _pendingRequestId = null;
        SetLoading(false);

        if (resp.Url.Length == 0)
        {
            Context.Toast(Loc.GetString("os-browser-bad-url"));
            return;
        }

        if (_pendingPushesHistory)
        {
            _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
            _history.Add(resp.Url);
            _historyIndex = _history.Count - 1;
        }
        else if (_historyIndex >= 0)
        {
            _history[_historyIndex] = resp.Url;
        }

        _page = resp;
        _address.Text = resp.DisplayUrl;
        _web.Url = resp.Url;

        UpdateButtons();
    }

    private void SetLoading(bool loading)
    {
        _status.Visible = loading;

        if (loading)
            _status.Text = Loc.GetString("os-browser-loading");

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var loading = _pendingRequestId != null;

        _back.Disabled = loading || _historyIndex <= 0;
        _forward.Disabled = loading || _historyIndex >= _history.Count - 1;
        _reload.Disabled = loading || _historyIndex < 0;
        _home.Disabled = loading;
    }
}
