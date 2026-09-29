using System.Numerics;
using Content.Shared.ADT.Alert.Click;
using Content.Shared.ADT.OfferItem;
using Content.Shared.ADT.CCVar;
using Content.Shared.Alert;
using Content.Shared.IdentityManagement;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Client.ADT.OfferItem;

public sealed class OfferItemSystem : SharedOfferItemSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eye = default!;

    private OfferItemWindow? _window;
    private bool _dismissed;
    private bool _suppressDismiss;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_cfg, ADTCCVars.OfferModeIndicatorsPointShow, OnShowOfferIndicatorsChanged, true);
        SubscribeLocalEvent<OfferItemComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    public override void Shutdown()
    {
        CloseWindow();
        _overlayManager.RemoveOverlay<OfferItemIndicatorsOverlay>();
        base.Shutdown();
    }

    private void OnAfterAutoHandleState(Entity<OfferItemComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (IsRelevant(ent.Owner))
            UpdateWindow();
    }

    protected override void OnOfferChanged(Entity<OfferItemComponent> ent)
    {
        if (IsRelevant(ent.Owner))
            UpdateWindow();
    }

    private bool IsRelevant(EntityUid uid)
    {
        if (_playerManager.LocalEntity is not { } local)
            return false;

        if (uid == local)
            return true;

        return TryComp<OfferItemComponent>(local, out var localComp) && localComp.Target == uid;
    }

    private void UpdateWindow()
    {
        if (_playerManager.LocalEntity is not { } local
            || !TryComp<OfferItemComponent>(local, out var comp)
            || !comp.IsInReceiveMode)
        {
            CloseWindow();
            return;
        }

        if (_dismissed
            || comp.Target is not { } giver
            || !TryComp<OfferItemComponent>(giver, out var giverComp)
            || giverComp.Item is not { } item)
            return;

        if (_window == null)
        {
            _window = new OfferItemWindow();
            _window.OnAccept += OnAcceptPressed;
            _window.OnDecline += OnDeclinePressed;
            _window.OnClose += OnWindowClosed;
        }

        _window.SetOffer(Identity.Name(giver, EntityManager, local), Identity.Name(item, EntityManager, local), item);

        if (!_window.IsOpen)
            _window.OpenCenteredAt(new Vector2(0.72f, 0.22f));
    }

    private void OnAcceptPressed()
    {
        RaisePredictiveEvent(new ClickAlertEvent(OfferAlert));
    }

    private void OnDeclinePressed()
    {
        _dismissed = true;
        RaiseNetworkEvent(new OfferItemDeclineEvent());
        CloseWindow(resetDismissed: false);
    }

    private void OnWindowClosed()
    {
        _window = null;

        if (!_suppressDismiss)
            _dismissed = true;
    }

    private void CloseWindow(bool resetDismissed = true)
    {
        if (resetDismissed)
            _dismissed = false;

        if (_window == null)
            return;

        _suppressDismiss = true;
        _window.Close();
        _suppressDismiss = false;
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
