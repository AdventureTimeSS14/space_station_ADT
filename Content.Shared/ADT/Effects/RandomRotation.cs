using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.ADT.Effects;

// ADT: перенесено из Content.Shared._Goobstation.Wizard.HighFrequencyBlade (эффекты еретика)
[RegisterComponent]
public sealed partial class RandomRotationComponent : Component
{
}

public sealed class RandomRotationSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomRotationComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomRotationComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsServer || IsClientSide(ent))
            _transform.SetLocalRotation(ent, _random.NextAngle());
    }
}
