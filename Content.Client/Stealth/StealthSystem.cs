using Content.Client.Interactable.Components;
<<<<<<< ours
using Content.Client.StatusIcon;
using Content.Shared.ADT.Stealth.Components;
||||||| base
using Content.Client.StatusIcon;
=======
using Content.Client.Graphics;
>>>>>>> theirs
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Stealth;

public sealed partial class StealthSystem : SharedStealthSystem
{
    private static readonly ProtoId<ShaderPrototype> Shader = "Stealth";

<<<<<<< ours
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
||||||| base
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
=======
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private SpriteSystem _sprite = default!;
>>>>>>> theirs

    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = ProtoMan.Index(Shader).InstanceUnique();

        SubscribeLocalEvent<StealthComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StealthComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StealthComponent, BeforePostShaderRenderEvent>(OnShaderRender);

        InitializeADT();    // ADT tweak
    }

    public override void SetEnabled(EntityUid uid, bool value, StealthComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.Enabled == value)
            return;

        base.SetEnabled(uid, value, component);
        SetShader(uid, value, component);
    }

    private void SetShader(EntityUid uid, bool enabled, StealthComponent? component = null, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref component, ref sprite, false))
            return;
        // ADT start
        if (!CheckStealthWhitelist(_player.LocalEntity, uid))
            enabled = false;
        // ADT end

        _sprite.SetColor((uid, sprite), Color.White);
        if (enabled)
        {
            _sprite.SetPostShader((uid, sprite), new SpriteComponent.PostShaderArgs(ContentPostShaderIds.Stealth, _shader)
            {
                GetScreenTexture = true,
                RaiseShaderEvent = true,
                Before = ContentPostShaderIds.BeforeOutlines,
            });
        }
        else
        {
            _sprite.RemovePostShader((uid, sprite), ContentPostShaderIds.Stealth);
        }

        if (!enabled)
        {
            if (component.HadOutline && !TerminatingOrDeleted(uid))
                EnsureComp<InteractionOutlineComponent>(uid);
            return;
        }

        if (HasComp<InteractionOutlineComponent>(uid))
            component.HadOutline = true;
    }

    private void OnStartup(EntityUid uid, StealthComponent component, ComponentStartup args)
    {
        SetShader(uid, component.Enabled, component);
    }

    private void OnShutdown(EntityUid uid, StealthComponent component, ComponentShutdown args)
    {
        if (!Terminating(uid))
            SetShader(uid, false, component);
    }

    private void OnShaderRender(EntityUid uid, StealthComponent component, BeforePostShaderRenderEvent args)
    {
        // ADT start
        if (!CheckStealthWhitelist(_player.LocalEntity, uid))
        {
            SetShader(uid, false);
            return;
        }

        // ADT end

        // Distortion effect uses screen coordinates. If a player moves, the entities appear to move on screen. this
        // makes the distortion very noticeable.

        // So we need to use relative screen coordinates. The reference frame we use is the parent's position on screen.
        // this ensures that if the Stealth is not moving relative to the parent, its relative screen position remains
        // unchanged.
        var parent = Transform(uid).ParentUid;
        if (!parent.IsValid())
            return; // should never happen, but lets not kill the client.
        var parentXform = Transform(parent);
        var reference = args.Viewport.WorldToLocal(_transformSystem.GetWorldPosition(parentXform));
        reference.X = -reference.X;
        var visibility = GetVisibility(uid, component);

        // actual visual visibility effect is limited to +/- 1.
        visibility = Math.Clamp(visibility, -1f, 1f);

        _shader.SetParameter("reference", reference);
        _shader.SetParameter("visibility", visibility);
        _shader.SetParameter("shimmer_frequency", component.ShimmerFrequency);

        visibility = MathF.Max(0, visibility);
        _sprite.SetColor((uid, args.Sprite), new Color(visibility, visibility, 1, 1));
    }
}
