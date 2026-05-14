using Content.Shared.ADT.Silicon.Components;
using Content.Shared.ADT.Silicon.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Timing;


namespace Content.Client.ADT.Overlays.Shaders;

public sealed class StaticOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> SeeingStaticShader = "SeeingStatic";
    private static readonly EntProtoId SeeingStaticEffect = "SeeingStatic";

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _sharedPlayerManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _staticShader;

    private (TimeSpan?, TimeSpan?)? _time;
    private float? _fullTimeLeft;
    private float? _curTimeLeft;

    public float MixAmount = 0;

    public StaticOverlay()
    {
        IoCManager.InjectDependencies(this);
        _staticShader = _prototypeManager.Index(SeeingStaticShader).InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var playerEntity = _sharedPlayerManager.LocalEntity;

        if (playerEntity == null)
            return;

        if (!_entityManager.TryGetComponent<SeeingStaticComponent>(playerEntity, out var staticComp))
            return;

        var status = _entityManager.EntitySysManager.GetEntitySystem<StatusEffectsSystem>();

        if (!status.TryGetTime(playerEntity.Value, SeeingStaticEffect, out var timeData))
            return;

        var timeTemp = (timeData.StartEffectTime, timeData.EndEffectTime);

        if (_time != timeTemp) // Resets the shader if the times change. This should factor in wheather it's a reset, or a increase, but I have a lot of cough syrup in me, so TODO.
        {
            _time = timeTemp;
            _fullTimeLeft = null;
            _curTimeLeft = null;
        }

        if (timeTemp.Item1.HasValue && timeTemp.Item2.HasValue)
        {
            _fullTimeLeft ??= (float)(timeTemp.Item2.Value - timeTemp.Item1.Value).TotalSeconds;
            _curTimeLeft ??= _fullTimeLeft;

            _curTimeLeft -= args.DeltaSeconds;

            MixAmount = Math.Clamp(_curTimeLeft.Value / _fullTimeLeft.Value * staticComp.Multiplier, 0, 1);
        }
        else
        {
            MixAmount = 0;
        }
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_sharedPlayerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        return MixAmount > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _staticShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _staticShader.SetParameter("mixAmount", MixAmount);
        handle.UseShader(_staticShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
