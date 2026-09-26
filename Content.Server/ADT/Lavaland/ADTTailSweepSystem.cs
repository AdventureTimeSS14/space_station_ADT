using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Actions;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTTailSweepSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    private readonly HashSet<Entity<MobStateComponent>> _targets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTTailSweepComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTTailSweepComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ADTTailSweepComponent, ADTTailSweepActionEvent>(OnTailSweep);
    }

    private void OnMapInit(Entity<ADTTailSweepComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<HumanoidProfileComponent>(ent.Owner, out var profile) || !ent.Comp.Species.Contains(profile.Species))
        {
            RemCompDeferred<ADTTailSweepComponent>(ent.Owner);
            return;
        }

        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        _popup.PopupEntity(Loc.GetString("adt-tail-sweep-granted"), ent.Owner, ent.Owner, PopupType.MediumCaution);
    }

    private void OnShutdown(Entity<ADTTailSweepComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionEntity);
    }

    private void OnTailSweep(Entity<ADTTailSweepComponent> ent, ref ADTTailSweepActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _targets.Clear();
        _lookup.GetEntitiesInRange(Transform(ent.Owner).Coordinates, ent.Comp.Range, _targets);

        foreach (var target in _targets)
        {
            if (target.Owner == ent.Owner || !HasComp<StaminaComponent>(target.Owner))
                continue;

            _stamina.TakeStaminaDamage(target.Owner, ent.Comp.StaminaDamage, source: ent.Owner);
            _popup.PopupEntity(Loc.GetString("adt-tail-sweep-hit", ("user", Identity.Entity(ent.Owner, EntityManager)), ("target", Identity.Entity(target.Owner, EntityManager))), target.Owner, PopupType.SmallCaution);
        }

        _stamina.TakeStaminaDamage(ent.Owner, ent.Comp.SelfStaminaDamage);
        _audio.PlayPvs(ent.Comp.Sound, ent.Owner);
    }
}
