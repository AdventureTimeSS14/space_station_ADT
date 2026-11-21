using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Hands;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Network;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed partial class RaggedSwordSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RaggedSwordComponent, GotEquippedHandEvent>(OnEquippedHand);
        SubscribeLocalEvent<RaggedSwordComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        SubscribeLocalEvent<RaggedSwordComponent, ComponentRemove>(OnRemove);
    }

    private void OnEquippedHand(Entity<RaggedSwordComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!_net.IsServer)
            return;

        ent.Comp.User = args.User;
        _status.TrySetStatusEffectDuration(args.User, ent.Comp.StatusEffect);
    }

    private void OnUnequippedHand(Entity<RaggedSwordComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!_net.IsServer)
            return;

        _status.TryRemoveStatusEffect(args.User, ent.Comp.StatusEffect);
        ent.Comp.User = null;
    }

    private void OnRemove(Entity<RaggedSwordComponent> ent, ref ComponentRemove args)
    {
        if (!_net.IsServer || ent.Comp.User is not { } user)
            return;

        _status.TryRemoveStatusEffect(user, ent.Comp.StatusEffect);
    }
}