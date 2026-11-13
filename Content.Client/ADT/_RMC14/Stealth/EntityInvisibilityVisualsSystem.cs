using Content.Client.Interactable.Components;
using Content.Shared._RMC14.Stealth;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Stealth;

public sealed class EntityInvisibilityVisualsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly ProtoId<ShaderPrototype> RMCInvisibleShader = "RMCInvisible";
    private const string RMCInvisibleShaderId = "RMCInvisibleShader";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityTurnInvisibleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<EntityTurnInvisibleComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EntityActiveInvisibleComponent, ComponentStartup>(OnActiveStartup);
        SubscribeLocalEvent<EntityActiveInvisibleComponent, ComponentShutdown>(OnActiveShutdown);
    }

    private void OnStartup(Entity<EntityTurnInvisibleComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var shader = _prototypes.Index(RMCInvisibleShader).InstanceUnique();
        _sprite.SetPostShader(sprite, new SpriteComponent.PostShaderArgs(RMCInvisibleShaderId, shader)
        {
            GetScreenTexture = true,
        });
    }

    private void OnShutdown(Entity<EntityTurnInvisibleComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent) || !TryComp(ent, out SpriteComponent? sprite))
            return;

        _sprite.RemovePostShader(sprite, RMCInvisibleShaderId);
    }

    private void OnActiveStartup(Entity<EntityActiveInvisibleComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<EntityTurnInvisibleComponent>(ent.Owner, out var turnInvisible))
            turnInvisible.HadOutline = RemCompDeferred<InteractionOutlineComponent>(ent.Owner);
    }

    private void OnActiveShutdown(Entity<EntityActiveInvisibleComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<EntityTurnInvisibleComponent>(ent.Owner, out var turnInvisible) && turnInvisible.HadOutline)
        {
            EnsureComp<InteractionOutlineComponent>(ent.Owner);
            turnInvisible.HadOutline = false;
        }
    }

    public override void Update(float frameTime)
    {
        var invisible = EntityQueryEnumerator<EntityTurnInvisibleComponent, SpriteComponent>();
        while (invisible.MoveNext(out var uid, out var comp, out var sprite))
        {
            var opacity = TryComp<EntityActiveInvisibleComponent>(uid, out var activeInvisible) ? activeInvisible.Opacity : 1;

            foreach (var entry in _sprite.GetPostShaders((uid, sprite)))
            {
                if (entry.Id == RMCInvisibleShaderId)
                    entry.Shader?.SetParameter("visibility", opacity);
            }
        }
    }
}
