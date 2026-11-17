// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using Content.Shared.Shaders;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Shaders.Systems;

public sealed partial class NaturalNightVisionOverlay : Overlay
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private static readonly ProtoId<ShaderPrototype> NaturalNightVision = "NaturalNightVision";
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _nightVisionShader;

    public float BrightnessGain = 3.0f;
    public float Contrast = 1.3f;
    public Color TintColor = Color.FromHex("#1c89f2");
    public float VisionRadius = 6.0f;

    public NaturalNightVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _nightVisionShader = _prototypeManager.Index(NaturalNightVision).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        if (!_entityManager.TryGetComponent<NaturalNightVisionComponent>(_playerManager.LocalEntity, out var naturalNight)
            || !naturalNight.Visible)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;

        _nightVisionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _nightVisionShader.SetParameter("brightness_gain", BrightnessGain);
        _nightVisionShader.SetParameter("vision_contrast", Contrast);
        _nightVisionShader.SetParameter("tint_color", TintColor);
        _nightVisionShader.SetParameter("vision_radius", VisionRadius);

        handle.UseShader(_nightVisionShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
