using Content.Server.ADT.Heretic.Systems.Moon;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;

namespace Content.Server.ADT.Heretic.EntitySystems;

public sealed class HereticMobInteractSystem : EntitySystem
{
    [Dependency] private readonly PhylacteryDrawSystem _phylactery = default!;
    [Dependency] private readonly MoonAmuletSystem _amulet = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<MobStateComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        _phylactery.OnMobInteractUsing(ent, ref args);
        if (args.Handled)
            return;

        _amulet.OnMobInteractUsing(ent, ref args);
    }
}
