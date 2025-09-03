using Content.Shared.Humanoid;
using Content.Shared.Devour.Components;

namespace Content.Server.Devour;

/// <summary>
/// Ensures a sane default vore role so the game does not error if none is picked.
/// If an entity has neither Predator nor Prey at map init, we default to Prey.
/// </summary>
public sealed class DevourRoleFallbackSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidAppearanceComponent, MapInitEvent>(OnHumanoidMapInit);
    }

    private void OnHumanoidMapInit(EntityUid uid, HumanoidAppearanceComponent comp, MapInitEvent args)
    {
        if (HasComp<PredatorComponent>(uid))
            return;
        if (HasComp<PreyComponent>(uid))
            return;

        // Default role if none selected
        EnsureComp<PreyComponent>(uid);
    }
}


