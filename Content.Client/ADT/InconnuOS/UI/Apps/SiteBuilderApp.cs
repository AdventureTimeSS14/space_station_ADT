using System.IO;
using System.Text;
using Content.Client.ADT.Sponsors;
using Content.Shared.ADT.InconnuOS;
using Content.Shared.ADT.InconnuOS.NanoNet;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Client.ADT.InconnuOS.UI.Apps;

public sealed class SiteBuilderApp : OsAppControl
{
    private readonly TextEdit _text;
    private readonly Control _contentSlot;
    private readonly LineEdit _prompt;
    private readonly LineEdit _domain;
    private readonly Label _status;
    private readonly Label _publishStatus;
    private readonly Button _save;
    private readonly Button _publish;
    private readonly Button _unpublish;
    private readonly Button _previewToggle;

    private OsWebView? _preview;
    private string _previewHtml = string.Empty;
    private int _previewRevision;
    private bool _previewing;

    private string? _path;
    private string _saved = string.Empty;
    private bool _dirty;
    private bool _closeAsked;

    private string? _publishedDomain;
    private int? _pendingRequestId;
    private bool _subscribed;

    private string? _pendingSave;

    public SiteBuilderApp()
    {
        _text = new TextEdit
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(4f, 2f, 4f, 2f),
        };

        _contentSlot = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            Children = { _text },
        };

        _save = OsWidgets.Small(Loc.GetString("os-notepad-save"));
        var saveAs = OsWidgets.Small(Loc.GetString("os-notepad-save-as"));
        _previewToggle = OsWidgets.Small(Loc.GetString("os-sitebuilder-preview"));

        _status = new Label
        {
            Modulate = OsStyle.TextDim,
            HorizontalExpand = true,
            ClipText = true,
        };

        _prompt = new LineEdit
        {
            HorizontalExpand = true,
            Visible = false,
            Margin = new Thickness(4f, 0f, 4f, 0f),
            PlaceHolder = Loc.GetString("os-notepad-prompt"),
        };

        var fileToolbar = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(4f, 4f, 4f, 2f),
            Children = { _save, saveAs, _previewToggle, _status },
        };

        _domain = new LineEdit
        {
            HorizontalExpand = true,
            Margin = new Thickness(0f, 0f, 4f, 0f),
            PlaceHolder = Loc.GetString("os-sitebuilder-domain-placeholder"),
        };

        _publish = OsWidgets.Small(Loc.GetString("os-sitebuilder-publish"));
        _unpublish = OsWidgets.Small(Loc.GetString("os-sitebuilder-unpublish"));

        var publishToolbar = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(4f, 0f, 4f, 4f),
            Children = { _domain, _publish, _unpublish },
        };

        _publishStatus = new Label
        {
            Modulate = OsStyle.TextDim,
            Margin = new Thickness(4f, 0f, 4f, 4f),
        };

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            Children = { fileToolbar, _prompt, publishToolbar, _publishStatus, _contentSlot },
        });

        _text.OnTextChanged += _ => OnEdited();
        _save.OnPressed += _ => Save();
        saveAs.OnPressed += _ => OpenPrompt();
        _prompt.OnTextEntered += _ => ConfirmPrompt();
        _previewToggle.OnPressed += _ => TogglePreview();
        _publish.OnPressed += _ => Publish();
        _unpublish.OnPressed += _ => Unpublish();

        RefreshPublishState();
    }

    public override void OnOpen(string? argument)
    {
        base.OnOpen(argument);

        if (!_subscribed)
        {
            _subscribed = true;
            Context.MessageReceived += OnServerMessage;
        }

        if (argument == null)
        {
            SetText(string.Empty);
            Refresh();
            return;
        }

        _path = argument;

        SetText(Context.FindFile(argument)?.Text ?? string.Empty);
        Refresh();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || !_subscribed)
            return;

        Context.MessageReceived -= OnServerMessage;
    }

    public override void OnStateChanged()
    {
        base.OnStateChanged();

        if (_path != null && Context.FindFile(_path) is { } file)
        {
            if (_pendingSave != null)
            {
                if (file.Text == _pendingSave)
                {
                    _saved = _pendingSave;
                    _pendingSave = null;
                    _dirty = Current() != _saved;
                }
            }
            else if (!_dirty && file.Text != _saved)
            {
                SetText(file.Text);
            }
        }

        Refresh();
    }

    private sealed record SiteBuilderState(string? Path, string Text, string Saved, string Domain, string? PublishedDomain);

    public override object? SaveState()
    {
        return new SiteBuilderState(_path, Current(), _saved, _domain.Text, _publishedDomain);
    }

    public override void LoadState(object state)
    {
        if (state is not SiteBuilderState saved)
            return;

        _path = saved.Path;
        _saved = saved.Saved;
        _text.TextRope = new Rope.Leaf(saved.Text);
        _dirty = saved.Text != saved.Saved;
        _domain.Text = saved.Domain;
        _publishedDomain = saved.PublishedDomain;

        RefreshPublishState();
        Refresh();
    }

    public override bool OnClosing()
    {
        if (!_dirty || _closeAsked)
            return true;

        _closeAsked = true;

        Context.Toast(Loc.GetString("os-notepad-unsaved"));

        return false;
    }

    private void OnServerMessage(BoundUserInterfaceMessage message)
    {
        if (message is not ADTOsNanoNetStatusMessage status)
            return;

        if (status.RequestId != _pendingRequestId)
            return;

        _pendingRequestId = null;

        if (status.Published)
        {
            _publishedDomain = status.Domain;
            _publishStatus.Text = Loc.GetString("os-sitebuilder-published",
                ("host", NanoNetDomain.GetHost(status.Domain)));
        }
        else
        {
            _publishedDomain = null;
            _publishStatus.Text = Loc.GetString("os-sitebuilder-not-published");
        }

        RefreshPublishState();
    }

    private void Publish()
    {
        if (_pendingRequestId != null)
            return;

        if (!NanoNetDomain.TryNormalizeLabel(_domain.Text, out var label, out var error))
        {
            Context.Toast(OsErrors.GetMessage(error, _domain.Text));
            return;
        }

        var html = Current();
        var maxSiteLength = IoCManager.Resolve<IConfigurationManager>().GetCVar(NanoNetCVars.MaxSiteLength);

        if (html.Length > maxSiteLength)
        {
            Context.Toast(OsErrors.GetMessage(OsValidationError.SiteTooLarge, maxSiteLength.ToString()));
            return;
        }

        _domain.Text = label;

        var id = Context.NextNanoNetRequestId();
        _pendingRequestId = id;

        Context.Send(new ADTOsNanoNetPublishMessage(id, label, html));

        RefreshPublishState();
    }

    private void Unpublish()
    {
        if (_pendingRequestId != null || _publishedDomain == null)
            return;

        var id = Context.NextNanoNetRequestId();
        _pendingRequestId = id;

        Context.Send(new ADTOsNanoNetUnpublishMessage(id, _publishedDomain));

        RefreshPublishState();
    }

    private void TogglePreview()
    {
        if (_previewing)
        {
            _previewing = false;

            if (_preview != null)
                _preview.Visible = false;

            _text.Visible = true;
            _previewToggle.Text = Loc.GetString("os-sitebuilder-preview");
            return;
        }

        _previewing = true;
        _text.Visible = false;

        if (_preview == null)
        {
            var preview = new OsWebView
            {
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            preview.Web.AddResourceRequestHandler(ctx =>
            {
                if (!ctx.IsNavigation)
                {
                    ctx.DoCancel();
                    return;
                }

                var bytes = Encoding.UTF8.GetBytes("﻿" + _previewHtml);
                ctx.DoRespondStream(new MemoryStream(bytes), "text/html");
            });

            _preview = preview;
            _contentSlot.AddChild(preview);
        }

        _preview.Visible = true;

        _previewHtml = Current();
        _previewRevision++;
        _preview.Web.Url = $"http://sitebuilder.local/preview#_r={_previewRevision}";

        _previewToggle.Text = Loc.GetString("os-sitebuilder-edit");
    }

    private void SetText(string text)
    {
        _saved = text;
        _dirty = false;

        _text.TextRope = new Rope.Leaf(text);
    }

    private void OnEdited()
    {
        _dirty = Current() != _saved;
        _closeAsked = false;

        Refresh();
    }

    private string Current()
    {
        return Rope.Collapse(_text.TextRope);
    }

    private void Save()
    {
        if (_path == null)
        {
            OpenPrompt();
            return;
        }

        var text = Current();

        var hasNanoNetLargeSites = IoCManager.Resolve<SponsorManager>().Data.NanoNetLargeSites;
        var limit = Context.State.Limits.MaxFileLength;

        if (hasNanoNetLargeSites)
        {
            var maxSiteLength = IoCManager.Resolve<IConfigurationManager>().GetCVar(NanoNetCVars.MaxSiteLength);
            limit = Math.Max(limit, maxSiteLength);
        }

        if (text.Length > limit)
        {
            Context.Toast(OsErrors.GetMessage(OsValidationError.FileTooLong, limit.ToString()));
            return;
        }

        Context.Send(new ADTOsFileWriteMessage(_path, OsFileKind.Text, text, null, hasNanoNetLargeSites));

        _pendingSave = text;

        Refresh();
    }

    private void OpenPrompt()
    {
        _prompt.Visible = true;
        _prompt.Text = _path == null ? string.Empty : OsPath.GetName(_path);

        _prompt.GrabKeyboardFocus();
    }

    private void ConfirmPrompt()
    {
        var name = _prompt.Text.Trim();

        if (!name.Contains('.'))
            name += ".html";

        if (!OsPath.IsValidName(name))
        {
            Context.Toast(OsErrors.GetMessage(OsValidationError.InvalidPath, name));
            return;
        }

        var folder = _path == null ? WritableRoot() : OsPath.GetParent(_path);

        _path = OsPath.Combine(folder, name);

        _prompt.Visible = false;
        _prompt.Text = string.Empty;

        Save();
    }

    private string WritableRoot()
    {
        foreach (var drive in Context.State.Drives)
        {
            if (!drive.ReadOnly)
                return OsPath.GetRoot(drive.Letter);
        }

        return "C:/";
    }

    private void RefreshPublishState()
    {
        var busy = _pendingRequestId != null;

        _publish.Disabled = busy;
        _unpublish.Disabled = busy || _publishedDomain == null;

        _publishStatus.Text = _publishedDomain == null
            ? Loc.GetString("os-sitebuilder-not-published")
            : Loc.GetString("os-sitebuilder-published", ("host", NanoNetDomain.GetHost(_publishedDomain)));
    }

    private void Refresh()
    {
        var name = _path == null
            ? Loc.GetString("os-notepad-new")
            : OsPath.GetName(_path);

        SetTitle(_dirty ? $"{name} *" : name);

        var file = _path == null ? null : Context.FindFile(_path);
        var writable = _path == null || Context.IsWritable(_path);

        _save.Disabled = !writable || file is { ReadOnly: true };

        _status.Text = Loc.GetString("os-notepad-status",
            ("length", Current().Length),
            ("limit", Context.State.Limits.MaxFileLength));
    }
}
