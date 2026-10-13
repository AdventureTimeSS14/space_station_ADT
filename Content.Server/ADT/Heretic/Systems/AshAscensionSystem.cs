using Content.Server.ADT.Heretic.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Heretic.Systems;

public sealed partial class AshAscensionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [ValidatePrototypeId<EntityPrototype>]
    private const string HeatDamageType = "Heat";

    private static readonly EntProtoId FirePrototype = "HereticFireAA";
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AshAscendedComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
    }

    private void OnBeforeDamage(Entity<AshAscendedComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled || args.Damage.Empty)
            return;

        if (args.Damage.DamageDict.TryGetValue(HeatDamageType, out var heat))
            args.Damage.DamageDict[HeatDamageType] = 0f;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

var query = EntityQueryEnumerator<AshAscendedComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            comp.Accumulator += frameTime;
            if (comp.Accumulator < comp.FireInterval)
                continue;

            comp.Accumulator = 0f;

            for (var i = 0; i < 3; i++)
            {
                var offset = _random.NextVector2(-comp.Radius, comp.Radius);
var coords = Transform(uid).Coordinates.Offset(offset);
                Spawn(FirePrototype, coords);
            }
        }
    }
}
