using Content.Shared.ADT.Crushers.Components;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Crushers.Systems;

public sealed class CrusherStaggerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTCrusherStaggerOnDetonateComponent, CrusherMarkDetonatedEvent>(OnDetonated);
        SubscribeLocalEvent<ADTCrusherStaggerOnDetonateComponent, TrophyHolderExamineEvent>(OnTrophyExamine);
    }

    private void OnDetonated(Entity<ADTCrusherStaggerOnDetonateComponent> ent, ref CrusherMarkDetonatedEvent args)
    {
        var stacks = EnsureComp<ADTCrusherStaggerStacksComponent>(args.Target);
        var now = _timing.CurTime;

        if (now - stacks.LastHit > ent.Comp.StackDecay)
            stacks.Stacks = 0;

        stacks.LastHit = now;
        stacks.Stacks++;

        var megafauna = HasComp<MegafaunaComponent>(args.Target);
        var required = megafauna ? ent.Comp.MegafaunaHits : ent.Comp.Hits;

        if (stacks.Stacks < required)
            return;

        stacks.Stacks = 0;

        if (megafauna)
        {
            _status.TryUpdateStatusEffectDuration(args.Target, ent.Comp.VulnerableEffect, ent.Comp.MegafaunaVulnerableDuration);
        }
        else
        {
            _stun.TryUpdateStunDuration(args.Target, ent.Comp.StunDuration);
            _status.TryUpdateStatusEffectDuration(args.Target, ent.Comp.VulnerableEffect, ent.Comp.StunDuration);
        }

        _audio.PlayPvs(ent.Comp.StaggerSound, args.Target);
        _popup.PopupEntity(Loc.GetString("adt-crusher-stagger-popup"), args.Target, PopupType.MediumCaution);
    }

    private void OnTrophyExamine(Entity<ADTCrusherStaggerOnDetonateComponent> ent, ref TrophyHolderExamineEvent args)
    {
        args.PushMarkup(Loc.GetString("adt-crusher-stagger-examine",
            ("hits", ent.Comp.Hits),
            ("stun", ent.Comp.StunDuration.TotalSeconds.ToString("0.#")),
            ("megafaunaHits", ent.Comp.MegafaunaHits),
            ("vulnerable", ent.Comp.MegafaunaVulnerableDuration.TotalSeconds.ToString("0.#"))));
    }
}
