using Content.Shared.ADT.EntityEffects;
using Content.Shared.EntityEffects;
using Content.Shared.NPC.Components;
using Content.Server.ADT.NPC;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class ChangeFactionEntityEffectSystem : EntityEffectSystem<NpcFactionMemberComponent, ChangeFactionEntityEffect>
{
    [Dependency] private readonly ChangeFactionStatusEffectSystem _changeFaction = default!;

    protected override void Effect(Entity<NpcFactionMemberComponent> entity, ref EntityEffectEvent<ChangeFactionEntityEffect> args)
    {
        var uid = entity.Owner;
        var effect = args.Effect;

        _changeFaction.TryChangeFaction(uid, effect.NewFaction, out _, effect.Duration);
    }
}