using Content.Shared.ADT.Xenobiology.Components;
using Robust.Shared.Random;

namespace Content.Shared.ADT.Xenobiology.Systems;

public sealed partial class RandomBreedOnSpawn : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly XenobiologySystem _xenobiologySystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomBreedOnSpawnComponent, MapInitEvent>(OnSlimeInit);
    }

    private void OnSlimeInit(EntityUid uid, RandomBreedOnSpawnComponent comp, ref MapInitEvent args)
    {
        if (!TryComp<SlimeComponent>(uid, out var slime))
        {
            return;
        }

        var selectedBreed = _random.Pick(comp.Mutations);
        _xenobiologySystem.DoBreeding(uid, slime.DefaultSlimeProto, selectedBreed);
    }
}
