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
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

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
            || !TryGetOffer(local, comp, out var giver, out var item))
        {
            ClearIcon();
            return;
        }

        if (!EnsureRoot())
            return;

        var receiving = giver != local;
        if (_icon == null || _icon.Anchor != giver)
        {
            _icon?.Orphan();
            _icon = new OfferItemIcon(giver);
            _icon.OnPressed += _ => OnIconPressed();
            _root!.AddChild(_icon);
        }

        var tooltip = receiving ? "offer-item-icon-tooltip" : "offer-item-icon-tooltip-giver";
        _icon.SetItem(item, Loc.GetString(tooltip, ("item", Identity.Name(item, EntityManager, local))));
    }

    private bool TryGetOffer(EntityUid local, OfferItemComponent comp, out EntityUid giver, out EntityUid item)
    {
        giver = default;
        item = default;

        if (comp.Target is not { } target)
            return false;

        if (comp.IsInReceiveMode)
        {
            if (!TryComp<OfferItemComponent>(target, out var giverComp) || giverComp.Item is not { } offered)
                return false;

            giver = target;
            item = offered;
            return true;
        }

        if (comp.Item is not { } own)
            return false;

        giver = local;
        item = own;
        return true;
    }

    private void OnIconPressed()
    {
        if (_icon == null || _icon.Anchor == _playerManager.LocalEntity)
            return;

        Accept();
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
