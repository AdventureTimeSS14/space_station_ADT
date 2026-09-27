using Content.Server.Temperature.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.ADT.Weapons.ChangeTemperatureOnHit;

// ADT: перенесено из Content.Goobstation.Server.Weapons.ChangeTemperatureOnHit (Еретик, ледяное копьё)
[RegisterComponent]
public sealed partial class ChangeTemperatureOnHitComponent : Component
{
    [DataField]
    public float Heat;

    [DataField]
    public bool IgnoreResistances = true;
}

public sealed class ChangeTemperatureOnHitSystem : EntitySystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeTemperatureOnHitComponent, MeleeHitEvent>(OnHit);
    }

    private void OnHit(Entity<ChangeTemperatureOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var comp = ent.Comp;

        foreach (var target in args.HitEntities)
        {
            _temperature.ChangeHeat(target, comp.Heat, comp.IgnoreResistances);
        }
    }
}
