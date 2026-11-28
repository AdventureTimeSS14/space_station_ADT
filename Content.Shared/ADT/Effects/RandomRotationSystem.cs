using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.ADT.Effects;

public sealed class RandomRotationSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomRotationComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomRotationComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient && !IsClientSide(ent))
            return;

        _transform.SetLocalRotation(ent, _random.NextAngle());
    }
}
