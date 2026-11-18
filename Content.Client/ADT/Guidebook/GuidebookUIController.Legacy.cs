using System.Numerics;
using Content.Client.Guidebook.Controls;
using Content.Shared.CCVar;
using Content.Shared.Guidebook;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Guidebook;

public sealed partial class GuidebookUIController
{
    private GuidebookWindow? _legacyGuideWindow;

    public void OpenLegacyGuidebook(
        Dictionary<ProtoId<GuideEntryPrototype>, GuideEntry> entries,
        List<ProtoId<GuideEntryPrototype>>? rootEntries = null,
        ProtoId<GuideEntryPrototype>? forceRoot = null,
        ProtoId<GuideEntryPrototype>? selected = null)
    {
        if (_guideWindow == null)
            return;

        DisposeLegacyGuidebook();
        _guideWindow.ClosePopout(false);
        _guideWindow.Close();
        _legacyGuideWindow = UIManager.CreateWindow<GuidebookWindow>();
        _legacyGuideWindow.OnClose += OnWindowClosed;
        _legacyGuideWindow.OnOpen += OnWindowOpen;
        _legacyGuideWindow.Title = Loc.GetString("adt-guidebook-legacy");

        if (selected is { } id && !entries.ContainsKey(id))
            selected = null;

        if (selected == null)
        {
            var defaultGuide = _configuration.GetCVar(CCVars.DefaultGuide);
            if (entries.ContainsKey(defaultGuide))
                selected = defaultGuide;
        }

        _legacyGuideWindow.UpdateGuides(entries, rootEntries, forceRoot, selected);
        var available = Vector2.Max(UIManager.WindowRoot.Size - new Vector2(24), Vector2.One);
        _legacyGuideWindow.MinSize = Vector2.Min(new Vector2(100, 200), available);
        _legacyGuideWindow.MaxSize = available;
        _legacyGuideWindow.SetSize = Vector2.Min(new Vector2(900, 700), available);
        _legacyGuideWindow.OpenCentered();

        var window = _legacyGuideWindow;
        UIManager.DeferAction(() =>
        {
            if (window.Disposed || !window.IsOpen || window.Parent == null)
                return;

            window.Measure(window.Parent.Size);
            window.RecenterWindow(new Vector2(0.5f, 0.5f));
        });
    }

    private bool ClosePopoutGuidebook()
    {
        if (_guideWindow is not { IsPoppedOut: true })
            return false;

        UIManager.ClickSound();
        _guideWindow.ClosePopout();
        return true;
    }

    private bool CloseLegacyGuidebook(bool playSound = false)
    {
        if (_legacyGuideWindow is not { IsOpen: true })
            return false;

        if (playSound)
            UIManager.ClickSound();

        _legacyGuideWindow.Close();
        return true;
    }

    private void DisposeLegacyGuidebook()
    {
        if (_legacyGuideWindow == null)
            return;

        _legacyGuideWindow.OnClose -= OnWindowClosed;
        _legacyGuideWindow.OnOpen -= OnWindowOpen;
        _legacyGuideWindow.Dispose();
        _legacyGuideWindow = null;
    }
}
