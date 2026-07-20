using Content.Client.Interactable.Components;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Viewport;
using Content.Shared.ADT.Light;
using Content.Shared.Examine;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Light;

public sealed partial class LightVisibilitySystem : EntitySystem
{
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private ProtoId<ShaderPrototype> _shaderProto = "LightVisibility";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightVisibilityComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LightVisibilityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LightVisibilityComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<LightVisibilityComponent, BeforePostShaderRenderEvent>(OnBeforePostShader);

        SubscribeLocalEvent<LightVisibilityComponent, ExamineAttemptEvent>(OnExamineAttempt);
    }

    private void OnStartup(Entity<LightVisibilityComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.HadOutline = HasComp<InteractionOutlineComponent>(ent.Owner);
        RemComp<InteractionOutlineComponent>(ent.Owner);

        var shader = _proto.Index(_shaderProto).InstanceUnique();
        shader.SetParameter("minLight", ent.Comp.MinLight);
        shader.SetParameter("maxLight", ent.Comp.MaxLight);
        shader.SetParameter("minAlpha", ent.Comp.MinAlpha);
        shader.SetParameter("maxAlpha", ent.Comp.MaxAlpha);
        shader.SetParameter("showInside", ent.Comp.ShowInside);
        shader.SetParameter("edgeSoftness", ent.Comp.EdgeSoftness);

        Comp<SpriteComponent>(ent.Owner).PostShader = shader;
    }

    private void OnShutdown(Entity<LightVisibilityComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent.Owner))
            return;

        if (ent.Comp.HadOutline)
            EnsureComp<InteractionOutlineComponent>(ent.Owner);

        Comp<SpriteComponent>(ent.Owner).PostShader = null;
    }

    private void OnAfterAutoHandleState(Entity<LightVisibilityComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var shader = Comp<SpriteComponent>(ent.Owner).PostShader;

        if (shader == null)
            return;

        shader.SetParameter("minLight", ent.Comp.MinLight);
        shader.SetParameter("maxLight", ent.Comp.MaxLight);
        shader.SetParameter("minAlpha", ent.Comp.MinAlpha);
        shader.SetParameter("maxAlpha", ent.Comp.MaxAlpha);
        shader.SetParameter("showInside", ent.Comp.ShowInside);
        shader.SetParameter("edgeSoftness", ent.Comp.EdgeSoftness);
    }

    private void OnBeforePostShader(Entity<LightVisibilityComponent> ent, ref BeforePostShaderRenderEvent args)
    {
        if (_eye.CurrentEye is not { } eye)
            return;

        if (args.Sprite.PostShader is not { } shader)
            return;

        shader.SetParameter("minAlpha", eye.DrawLight ? ent.Comp.MinAlpha : 1f);
        shader.SetParameter("maxAlpha", eye.DrawLight ? ent.Comp.MaxAlpha : 1f);
    }

    private void OnExamineAttempt(Entity<LightVisibilityComponent> ent, ref ExamineAttemptEvent args)
    {
        if (ent.Comp.BlockExamine)
            args.Cancel();
    }
}
