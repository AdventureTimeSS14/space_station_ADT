// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using Content.Shared.NullRod.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.NullRod;

public sealed partial class NullDamageOverlay : Overlay
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    private static readonly ProtoId<ShaderPrototype> NullDamageShader = "NullDamage";
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _shader;

    private float _currentRatio;
    private float _targetRatio;

    public NullDamageOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(NullDamageShader).InstanceUnique();
        _currentRatio = 0f;
        _targetRatio = 0f;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var playerEntity = _playerManager.LocalEntity;
        var frameTime = (float)_gameTiming.CurTime.TotalSeconds;

        if (playerEntity == null || !_entityManager.TryGetComponent<NullDamageComponent>(playerEntity, out var nullDamage))
        {
            _targetRatio = 0f;
            UpdateRatio(frameTime);
            _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _shader.SetParameter("ratio", _currentRatio);

            var handle = args.ScreenHandle;
            handle.UseShader(_shader);
            handle.DrawRect(args.ViewportBounds, Color.White);
            handle.UseShader(null);
            return;
        }

        var rawRatio = nullDamage.MaxNullDamage > 0
            ? (float)(nullDamage.NullDamage / nullDamage.MaxNullDamage)
            : 0f;

        _targetRatio = Math.Clamp(rawRatio, 0f, 0.85f);
        UpdateRatio(frameTime);

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("ratio", _currentRatio);

        var drawHandle = args.ScreenHandle;
        drawHandle.UseShader(_shader);
        drawHandle.DrawRect(args.ViewportBounds, Color.White);
        drawHandle.UseShader(null);
    }

    private void UpdateRatio(float frameTime)
    {
        const float maxChangePerSecond = 3f;
        var maxDelta = maxChangePerSecond * frameTime;

        var difference = _targetRatio - _currentRatio;
        var delta = Math.Clamp(difference, -maxDelta, maxDelta);

        float smoothingSpeed = 5f;
        delta *= smoothingSpeed * frameTime;
        delta = Math.Clamp(delta, -maxDelta, maxDelta);

        _currentRatio += delta;
        _currentRatio = Math.Clamp(_currentRatio, 0f, 0.85f);
    }
}
