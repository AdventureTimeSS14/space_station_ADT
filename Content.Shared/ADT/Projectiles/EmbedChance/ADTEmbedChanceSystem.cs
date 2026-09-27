using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Projectiles.EmbedChance;

public sealed class ADTEmbedChanceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTEmbedChanceComponent, ThrowDoHitEvent>(OnThrowDoHit, before: new[] { typeof(SharedProjectileSystem) });
    }

    private void OnThrowDoHit(Entity<ADTEmbedChanceComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!TryComp<EmbeddableProjectileComponent>(ent, out var embeddable))
            return;

        var embed = _whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, args.Target)
            && SharedRandomExtensions.PredictedProb(_timing, ent.Comp.Chance, GetNetEntity(ent), GetNetEntity(args.Target));

        if (embeddable.EmbedOnThrow == embed)
            return;

        embeddable.EmbedOnThrow = embed;
        Dirty(ent, embeddable);
    }
}
