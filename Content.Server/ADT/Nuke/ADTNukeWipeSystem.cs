// Портировано из RMC-14 (MIT, Copyright (c) 2023-2026 RMC-14): RMCNukeSystem.
using Content.Server.Nuke;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;

namespace Content.Server.ADT.Nuke;

/// <summary>
/// Ядерный взрыв наносит большой дамаг всем на том же гриде, на котором и ядерка.
/// </summary>
public sealed class ADTNukeWipeSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;

    private static readonly DamageSpecifier NukeDamage = new()
    {
        DamageDict =
        {
            ["Blunt"] = 100000,
            ["Heat"] = 100000,
        },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
    }

    private void OnNukeExploded(NukeExplodedEvent ev)
    {
        if (ev.OwningStation is not { } grid)
            return;

        KillEverythingOnGrid(grid);
    }

    private void KillEverythingOnGrid(EntityUid grid)
    {
        var query = EntityQueryEnumerator<MobStateComponent, DamageableComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var xform))
        {
            if (xform.GridUid != grid)
                continue;

            _damageable.TryChangeDamage(uid, NukeDamage, ignoreResistances: true);
        }
    }
}
