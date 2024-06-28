using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.IdentityManagement;
using Robust.Shared.Network;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;
using Content.Shared.Traits.Assorted;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This handles permanent blindness, both the examine and the actual effect.
/// </summary>
public sealed class GlassesSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly BlindableSystem _blinding = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GlassesComponent, GotEquippedEvent>(Equipped);
        SubscribeLocalEvent<GlassesComponent, GotUnequippedEvent>(Unequipped);

    }

    private void Equipped(EntityUid uid, GlassesComponent component, ref GotEquippedEvent args)
    {
        if (!TryComp<MyopiaComponent>(args.Equipee, out var myopia))
            return;
        myopia.Active = false;
        if (!TryComp<BlindableComponent>(args.Equipee, out var blindable))
            return;
        _blinding.AdjustEyeDamage((args.Equipee, blindable), -myopia.EyeDamage);
    }

    private void Unequipped(EntityUid uid, GlassesComponent component, ref GotUnequippedEvent args)
    {
        if (!TryComp<MyopiaComponent>(args.Equipee, out var myopia))
            return;
        myopia.Active = true;
        if (!TryComp<BlindableComponent>(args.Equipee, out var blindable))
            return;
        _blinding.AdjustEyeDamage((args.Equipee, blindable), myopia.EyeDamage);
    }
}
