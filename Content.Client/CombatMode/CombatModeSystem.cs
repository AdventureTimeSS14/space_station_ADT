using Content.Client.Hands.Systems;
using Content.Shared.CCVar;
using Content.Shared.CombatMode;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Content.Shared.ADT.CombatMode;
using Content.Shared.ADT.CCVar;
using Robust.Shared.Prototypes;

namespace Content.Client.CombatMode;

public sealed partial class CombatModeSystem : SharedCombatModeSystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IEyeManager _eye = default!;
    #region Starlight
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IClyde _clyde = default!;
    private bool _lastState = false;
    private string _rangedSight = "GunSight";
    private string _meleeSight = "MeleeSight";
    private float _scale = 0.6f;
    private float _offset = 0.5f;
    private bool _rangedRotation = true;
    private bool _meleeRotation = true;
    private bool _showBoltIndicator = true;
    private bool _showJamIndicator = true;
    private bool _showHeatIndicator = true;
    private Color _main = Color.White.WithAlpha(0.3f);
    private Color _second = Color.Black.WithAlpha(0.5f);
    #endregion

    /// <summary>
    /// Raised whenever combat mode changes.
    /// </summary>
    public event Action<bool>? LocalPlayerCombatModeUpdated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeComponent, AfterAutoHandleStateEvent>(OnHandleState);

        Subs.CVar(_cfg, CCVars.CombatModeIndicatorsPointShow, OnShowCombatIndicatorsChanged, true);
        // Starlight-start
        Subs.CVar(_cfg, ADTCCVars.RangedSight, OnRangedSightChanged, true);
        Subs.CVar(_cfg, ADTCCVars.RangedSightScale, OnRangedSightScaleChanged, true);
        Subs.CVar(_cfg, ADTCCVars.RangedSightOffset, OnRangedSightOffsetChanged, true);
        Subs.CVar(_cfg, ADTCCVars.SightMainColor, OnSightMainColorChanged, true);
        Subs.CVar(_cfg, ADTCCVars.SightSecondColor, OnSightSecondColorChanged, true);
        Subs.CVar(_cfg, ADTCCVars.MeleeSight, OnMeleeSightChanged, true);
        Subs.CVar(_cfg, ADTCCVars.RangedSightRotation, OnRangedRotationChanged, true);
        Subs.CVar(_cfg, ADTCCVars.MeleeSightRotation, OnMeleeRotationChanged, true);
        Subs.CVar(_cfg, ADTCCVars.SightShowBoltIndicator, OnShowBoltIndicatorChanged, true);
        Subs.CVar(_cfg, ADTCCVars.SightShowJamIndicator, OnShowJamIndicatorChanged, true);
        Subs.CVar(_cfg, ADTCCVars.SightShowHeatIndicator, OnShowHeatIndicatorChanged, true);
        // Starlight-end
    }

    private void OnHandleState(EntityUid uid, CombatModeComponent component, ref AfterAutoHandleStateEvent args)
// ADT-Tweak Start
//     {
//         UpdateHud(uid);
//     }
// ADT-Tweak End
        => UpdateHud(uid); // Starlight-edit: lambda

    public override void Shutdown()
    {
        _overlayManager.RemoveOverlay<CombatModeIndicatorsOverlay>();

        base.Shutdown();
    }

    public bool IsInCombatMode()
    {
        var entity = _playerManager.LocalEntity;

        if (entity == null)
            return false;

        return IsInCombatMode(entity.Value);
    }

    public override void SetInCombatMode(EntityUid entity, bool value, CombatModeComponent? component = null)
    {
        base.SetInCombatMode(entity, value, component);
        UpdateHud(entity);
    }

    private void UpdateHud(EntityUid entity)
    {
        if (entity != _playerManager.LocalEntity || !Timing.IsFirstTimePredicted)
        {
            return;
        }

        var inCombatMode = IsInCombatMode();
        LocalPlayerCombatModeUpdated?.Invoke(inCombatMode);
    }

// ADT-Tweak Start
//     private void OnShowCombatIndicatorsChanged(bool isShow)
// ADT-Tweak End
    private void OnShowCombatIndicatorsChanged(bool isShow) // Starlight-edit: moved into another method
        => UpdateCombatIndicators(isShow);

    #region Starlight

    private void OnRangedSightChanged(string sight)
    {
        _rangedSight = sight;
        UpdateCombatIndicators();
    }

    private void OnRangedSightScaleChanged(int scale)
    {
        var fScale = scale * 0.01f;
        _scale = fScale;
        UpdateCombatIndicators();
    }

    private void OnRangedSightOffsetChanged(int offset)
    {
        var fOffset = offset * 0.01f;
        _offset = fOffset;
        UpdateCombatIndicators();
    }

    private void OnSightMainColorChanged(string color)
    {
        _main = Color.FromHex(color).WithAlpha(0.3f);
        UpdateCombatIndicators();
    }

    private void OnSightSecondColorChanged(string color)
    {
        _second = Color.FromHex(color).WithAlpha(0.5f);
        UpdateCombatIndicators();
    }

    private void OnMeleeSightChanged(string sight)
    {
        _meleeSight = sight;
        UpdateCombatIndicators();
    }

    private void OnRangedRotationChanged(bool rotation)
    {
// ADT-Tweak Start
//         if (isShow)
// ADT-Tweak End
        _rangedRotation = rotation;
        UpdateCombatIndicators();
    }

    private void OnShowBoltIndicatorChanged(bool show)
    {
        _showBoltIndicator = show;
        UpdateCombatIndicators();
    }

    private void OnShowJamIndicatorChanged(bool show)
    {
        _showJamIndicator = show;
        UpdateCombatIndicators();
    }

    private void OnShowHeatIndicatorChanged(bool show)
    {
        _showHeatIndicator = show;
        UpdateCombatIndicators();
    }

    private void OnMeleeRotationChanged(bool rotation)
    {
        _meleeRotation = rotation;
        UpdateCombatIndicators();
    }

    private void UpdateCombatIndicators(bool? isShow = null)
    {
        if (isShow != null && isShow != _lastState)
            _lastState = isShow.Value;

        if ((isShow == null || !_lastState) && _overlayManager.HasOverlay<CombatModeIndicatorsOverlay>())
            _overlayManager.RemoveOverlay<CombatModeIndicatorsOverlay>();
        if ((isShow == null || _lastState) && _prototypeManager.TryIndex<SightPrototype>(_rangedSight, out var ranged) && _prototypeManager.TryIndex<SightPrototype>(_meleeSight, out var melee))
        {
            _overlayManager.AddOverlay(new CombatModeIndicatorsOverlay(
                _inputManager,
                EntityManager,
                _prototypeManager,
                _eye,
                this,
// ADT-Tweak Start
//                 EntityManager.System<HandsSystem>()));
//         }
//         else
//         {
//             _overlayManager.RemoveOverlay<CombatModeIndicatorsOverlay>();
// ADT-Tweak End
                EntityManager.System<HandsSystem>(),
                _clyde,
                ranged,
                melee,
                _scale,
                _offset,
                _main,
                _second,
                _rangedRotation,
                _meleeRotation,
                _showBoltIndicator,
                _showJamIndicator,
                _showHeatIndicator));
        }
    }

    #endregion
}
