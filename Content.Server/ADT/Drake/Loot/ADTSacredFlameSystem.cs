using Content.Server.Atmos.EntitySystems;
using Content.Shared.ADT.Drake.Loot;
using Content.Shared.ADT.Flammability;
using Content.Shared.Atmos.Components;
using Content.Shared.Magic.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;

namespace Content.Server.ADT.Drake.Loot;

public sealed class ADTSacredFlameSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private readonly HashSet<Entity<MobStateComponent>> _targets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTSacredFlameSpellEvent>(OnSacredFlame);
    }

    private void OnSacredFlame(ADTSacredFlameSpellEvent args)
    {
        if (args.Handled)
            return;

        var ev = new BeforeCastSpellEvent(args.Performer);
        RaiseLocalEvent(args.Action, ref ev);

        if (ev.Cancelled)
            return;

        args.Handled = true;

        var caster = args.Performer;

        _targets.Clear();
        _lookup.GetEntitiesInRange(Transform(caster).Coordinates, args.Range, _targets);

        foreach (var target in _targets)
        {
            if (target.Owner == caster || !TryComp<FlammableComponent>(target, out var flammable))
                continue;

            _flammable.AdjustFireStacks(target, args.FireStacks, flammable);
            _flammable.Ignite(target, caster, flammable, caster);
        }

        if (!HasComp<FireImmunityComponent>(caster))
        {
            EntityManager.AddComponents(caster, args.ResistComponents, false);
            _popup.PopupEntity(Loc.GetString("adt-sacred-flame-resist"), caster, caster);
        }

        if (TryComp<FlammableComponent>(caster, out var casterFlammable))
            _flammable.Ignite(caster, caster, casterFlammable, caster);
    }
}
