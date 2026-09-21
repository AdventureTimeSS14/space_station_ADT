using Content.Shared.ADT.EntityEffects;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.EntityEffects;
using Content.Shared.Tag;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class MakeUnreactiveEntityEffectSystem : EntityEffectSystem<ReactiveComponent, MakeUnreactiveEntityEffect>
{
    [Dependency] private readonly TagSystem _tag = default!;

    protected override void Effect(Entity<ReactiveComponent> entity, ref EntityEffectEvent<MakeUnreactiveEntityEffect> args)
    {
        var uid = entity.Owner;
        RemComp<ReactiveComponent>(uid);
        _tag.AddTag(uid, "Trash");
    }
}