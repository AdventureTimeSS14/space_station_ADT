using Content.Shared.ADT.OfferItem;
using Content.Shared.ADT.CCVar;
using Content.Shared.Alert;
using Content.Shared.IdentityManagement;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client.ADT.OfferItem;

public sealed class OfferItemSystem : SharedOfferItemSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private LayoutContainer? _root;
    private OfferItemIcon? _icon;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_cfg, ADTCCVars.OfferModeIndicatorsPointShow, OnShowOfferIndicatorsChanged, true);
    }

    public override void Shutdown()
    {
        ClearIcon();
        _overlayManager.RemoveOverlay<OfferItemIndicatorsOverlay>();
        base.Shutdown();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (_playerManager.LocalEntity is not { } local
            || !TryComp<OfferItemComponent>(local, out var comp)
            || !comp.IsInReceiveMode
            || comp.Target is not { } giver
            || !TryComp<OfferItemComponent>(giver, out var giverComp)
            || giverComp.Item is not { } item)
        {
            ClearIcon();
            return;
        }

        if (!EnsureRoot())
            return;

        if (_icon == null || _icon.Anchor != giver)
        {
            _icon?.Orphan();
            _icon = new OfferItemIcon(giver);
            _icon.OnPressed += _ => Accept();
            _root!.AddChild(_icon);
        }

        _icon.SetItem(item, Loc.GetString("offer-item-icon-tooltip", ("item", Identity.Name(item, EntityManager, local))));
    }

    private void Accept()
    {
        RaisePredictiveEvent(new ClickAlertEvent(OfferAlert));
    }

    private bool EnsureRoot()
    {
        if (_ui.ActiveScreen?.FindControl<LayoutContainer>("ViewportContainer") is not { } viewport)
        {
            ClearIcon();
            return false;
        }

        if (_root?.Parent == viewport)
            return true;

        _root?.Orphan();
        _root = new LayoutContainer { MouseFilter = Control.MouseFilterMode.Ignore };
        viewport.AddChild(_root);
        LayoutContainer.SetAnchorPreset(_root, LayoutContainer.LayoutPreset.Wide);
        _root.SetPositionLast();
        _icon = null;
        return true;
    }

    private void ClearIcon()
    {
        _icon?.Orphan();
        _icon = null;
        _root?.Orphan();
        _root = null;
    }

    public bool IsInOfferMode()
    {
        var entity = _playerManager.LocalEntity;

        return entity is not null && IsInOfferMode(entity.Value);
    }

    private void OnShowOfferIndicatorsChanged(bool isShow)
    {
        if (isShow)
            _overlayManager.AddOverlay(new OfferItemIndicatorsOverlay(_inputManager, EntityManager, _eye, this));
        else
            _overlayManager.RemoveOverlay<OfferItemIndicatorsOverlay>();
    }
}
