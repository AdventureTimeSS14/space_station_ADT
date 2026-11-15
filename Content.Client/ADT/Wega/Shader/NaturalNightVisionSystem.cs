// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Shared.Actions;
using Content.Shared.Shaders;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Shaders.Systems;

public sealed partial class NaturalNightVisionSystem : EntitySystem
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private ILightManager _lightManager = default!;
    [Dependency] private SharedActionsSystem _action = default!;

    private NaturalNightVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeLocalEvent<NaturalNightVisionComponent, ComponentInit>(OnNightVisionInit);
        SubscribeLocalEvent<NaturalNightVisionComponent, ComponentShutdown>(OnNightVisionShutdown);
        SubscribeLocalEvent<NaturalNightVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NaturalNightVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<NaturalNightVisionComponent, AfterAutoHandleStateEvent>(OnComponentStateUpdated);
        SubscribeLocalEvent<NaturalNightVisionComponent, ToggleNaturalNightVisionEvent>(OnToggleNaturalNightVision);
    }

    private void UpdateLighting(bool isActive)
    {
        _lightManager.DrawLighting = isActive;
    }

    private void OnNightVisionInit(EntityUid uid, NaturalNightVisionComponent component, ComponentInit args)
    {
        if (_playerManager.LocalEntity == uid && component.Visible)
        {
            UpdateLighting(false);
            UpdateOverlayParameters(component);
            _overlayManager.AddOverlay(_overlay);
        }
    }

    private void OnNightVisionShutdown(EntityUid uid, NaturalNightVisionComponent component, ComponentShutdown args)
    {
        if (_playerManager.LocalEntity == uid)
        {
            UpdateLighting(true);
            _overlayManager.RemoveOverlay(_overlay);
            if (component.Action == null)
                return;

            _action.RemoveAction(uid, component.Action.Value);
        }
    }

    private void OnPlayerAttached(EntityUid uid, NaturalNightVisionComponent component, LocalPlayerAttachedEvent args)
    {
        if (!component.Visible)
            return;

        UpdateLighting(false);
        UpdateOverlayParameters(component);
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, NaturalNightVisionComponent component, LocalPlayerDetachedEvent args)
    {
        UpdateLighting(true);
        _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnComponentStateUpdated(EntityUid uid, NaturalNightVisionComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (_playerManager.LocalEntity == uid)
        {
            UpdateOverlayParameters(component);
        }
    }

    private void OnToggleNaturalNightVision(EntityUid uid, NaturalNightVisionComponent component, ToggleNaturalNightVisionEvent args)
    {
        if (_playerManager.LocalEntity == uid)
        {
            component.Visible = !component.Visible;

            UpdateLighting(!component.Visible);
            if (component.Visible)
            {
                UpdateOverlayParameters(component);
                _overlayManager.AddOverlay(_overlay);
            }
            else
            {
                _overlayManager.RemoveOverlay(_overlay);
            }
        }
    }

    private void UpdateOverlayParameters(NaturalNightVisionComponent component)
    {
        _overlay.BrightnessGain = component.BrightnessGain;
        _overlay.Contrast = component.Contrast;
        _overlay.TintColor = component.TintColor;
        _overlay.VisionRadius = component.VisionRadius;
    }
}
