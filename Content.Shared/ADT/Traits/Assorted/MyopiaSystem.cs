using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.IdentityManagement;
using Robust.Shared.Network;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This handles permanent blindness, both the examine and the actual effect.
/// </summary>
public sealed class MyopiaSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly BlindableSystem _blinding = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MyopiaComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MyopiaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MyopiaComponent, EyeDamageChangedEvent>(OnDamageChanged);

    }

    private void OnStartup(EntityUid uid, MyopiaComponent component, ComponentStartup args)
    {
        if (!TryComp<BlindableComponent>(uid, out var blindable))
            return;
        _blinding.AdjustEyeDamage((uid, blindable), component.EyeDamage);
    }

    private void OnShutdown(EntityUid uid, MyopiaComponent component, ComponentShutdown args)
    {
        if (!TryComp<BlindableComponent>(uid, out var blindable))
            return;
        _blinding.AdjustEyeDamage((uid, blindable), -component.EyeDamage);
    }

    private void OnDamageChanged(EntityUid uid, MyopiaComponent component, ref EyeDamageChangedEvent args)
    {
        if (!TryComp<BlindableComponent>(uid, out var blindable))
            return;
        if (!component.Active)
            return;
        if (blindable.EyeDamage < component.EyeDamage)
            _blinding.AdjustEyeDamage((uid, blindable), (component.EyeDamage - blindable.EyeDamage));

    }
}
