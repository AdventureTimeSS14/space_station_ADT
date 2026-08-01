using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.MartialArts;

public partial class SharedMartialArtsSystem
{
    private void InitializeCookbookTechnique()
    {
        SubscribeLocalEvent<CanPerformComboComponent, CookbookChopComboPerformedEvent>(OnCookbookChopCombo);
        SubscribeLocalEvent<CanPerformComboComponent, CookbookSpinComboPerformedEvent>(OnCookbookSpinCombo);
        SubscribeLocalEvent<CanPerformComboComponent, CookbookRollComboPerformedEvent>(OnCookbookRollCombo);
        SubscribeLocalEvent<CanPerformComboComponent, CookbookSqueezeComboPerformedEvent>(OnCookbookSqueezeCombo);

        SubscribeLocalEvent<GrantCookbookTechniqueComponent, UseInHandEvent>(OnGrantCQCUse);
    }

    private void OnCookbookChopCombo(Entity<CanPerformComboComponent> ent, ref CookbookChopComboPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        _stun.TryKnockdown(target, TimeSpan.FromSeconds(proto.ParalyzeTime), true, true, proto.DropItems);
        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);
        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, Loc.GetString("cookbook-technique-combo-chop"));
        ent.Comp.LastAttacks.Clear();
    }

    private void OnCookbookSpinCombo(Entity<CanPerformComboComponent> ent, ref CookbookSpinComboPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        _stun.TryUpdateStunDuration(target, TimeSpan.FromSeconds(proto.ParalyzeTime));
        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);
        ComboPopup(ent, target, Loc.GetString("cookbook-technique-combo-spin"));
        ent.Comp.LastAttacks.Clear();
    }

    private void OnCookbookRollCombo(Entity<CanPerformComboComponent> ent, ref CookbookRollComboPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage);
        _hands.TryDrop(target);
        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);
        ComboPopup(ent, target, Loc.GetString("cookbook-technique-combo-roll"));
        ent.Comp.LastAttacks.Clear();
    }

    private void OnCookbookSqueezeCombo(Entity<CanPerformComboComponent> ent, ref CookbookSqueezeComboPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        _stun.TryKnockdown(target, TimeSpan.FromSeconds(proto.ParalyzeTime), true, true, proto.DropItems);
        _hands.TryDrop(target);
        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);
        ComboPopup(ent, target, Loc.GetString("cookbook-technique-combo-squeeze"));
        ent.Comp.LastAttacks.Clear();
    }
}
