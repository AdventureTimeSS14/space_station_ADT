using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Heretic;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed class HereticCloakSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HereticCloakedStatusEffectComponent, StatusEffectAppliedEvent>(OnApply);
        SubscribeLocalEvent<HereticCloakedStatusEffectComponent, StatusEffectRemovedEvent>(OnRemove);
        SubscribeLocalEvent<StatusEffectContainerComponent, HereticLostFocusEvent>(OnLoseFocus);
    }

    private void OnLoseFocus(Entity<StatusEffectContainerComponent> ent, ref HereticLostFocusEvent args)
    {
        if (!_status.TryEffectsWithComp<HereticCloakedStatusEffectComponent>(ent, out var effects))
            return;
        foreach (var effect in effects)
        {
            if (effect.Comp1.LoseFocusMessage is { } message)
                _popup.PopupPredicted(Loc.GetString(message), ent, ent);
            if (effect.Comp1.RequiresFocus)
                PredictedQueueDel(effect.Owner);
        }
    }

    private void OnRemove(Entity<HereticCloakedStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_net.IsServer)
            _audio.PlayPvs(ent.Comp.UncloakSound, args.Target);
    }

    private void OnApply(Entity<HereticCloakedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_net.IsServer)
            _audio.PlayPvs(ent.Comp.CloakSound, args.Target);
    }
}
