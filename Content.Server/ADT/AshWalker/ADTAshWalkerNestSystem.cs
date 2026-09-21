using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.AshWalker;

public sealed class ADTAshWalkerNestSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly SoundSpecifier ConsumeSound = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTAshWalkerNestComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ADTAshWalkerNestComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.Interval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTAshWalkerNestComponent>();

        while (query.MoveNext(out var uid, out var nest))
        {
            if (now < nest.NextUpdate)
                continue;

            nest.NextUpdate = now + nest.Interval;

            Consume((uid, nest));
            SpawnEgg((uid, nest));
            Heal((uid, nest));
        }
    }

    private void Consume(Entity<ADTAshWalkerNestComponent> ent)
    {
        var bodies = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(Transform(ent.Owner).Coordinates, ent.Comp.ConsumeRange, bodies);

        foreach (var body in bodies)
        {
            if (!_mobState.IsIncapacitated(body.Owner, body.Comp))
                continue;

            _popup.PopupEntity(
                Loc.GetString("adt-ash-walker-nest-consume", ("target", body.Owner)),
                ent.Owner,
                PopupType.MediumCaution);
            _audio.PlayPvs(ConsumeSound, ent.Owner);

            ent.Comp.MeatCounter += HasComp<MegafaunaComponent>(body.Owner)
                ? ent.Comp.MeatPerMegafauna
                : ent.Comp.MeatPerBody;

            _gibbing.Gib(body.Owner);

            Repair(ent);
        }
    }

    private void SpawnEgg(Entity<ADTAshWalkerNestComponent> ent)
    {
        if (ent.Comp.MeatCounter < ent.Comp.MeatPerEgg)
            return;

        var proto = NeedsShaman() ? ent.Comp.ShamanEgg : ent.Comp.Egg;
        var dir = (Direction)_random.Next(8);
        var coords = Transform(ent.Owner).Coordinates.Offset(dir.ToIntVec());

        Spawn(proto, coords);

        _popup.PopupEntity(Loc.GetString("adt-ash-walker-nest-egg"), ent.Owner, PopupType.LargeCaution);
        ent.Comp.MeatCounter -= ent.Comp.MeatPerEgg;
    }

    private bool NeedsShaman()
    {
        var shamans = EntityQueryEnumerator<ADTAshWalkerComponent, MobStateComponent>();
        while (shamans.MoveNext(out var uid, out var walker, out var state))
        {
            if (walker.Shaman && !_mobState.IsDead(uid, state))
                return false;
        }

        var eggs = EntityQueryEnumerator<ADTAshWalkerEggComponent>();
        while (eggs.MoveNext(out _, out var egg))
        {
            if (egg.Shaman)
                return false;
        }

        return true;
    }

    private void Heal(Entity<ADTAshWalkerNestComponent> ent)
    {
        if (ent.Comp.AuraHealing.Empty)
            return;

        var tribe = new HashSet<Entity<ADTAshWalkerComponent>>();
        _lookup.GetEntitiesInRange(Transform(ent.Owner).Coordinates, ent.Comp.HealRange, tribe);

        foreach (var walker in tribe)
        {
            if (_mobState.IsDead(walker.Owner))
                continue;

            _damageable.TryChangeDamage(walker.Owner, ent.Comp.AuraHealing, true, origin: ent.Owner);
        }
    }

    private void Repair(Entity<ADTAshWalkerNestComponent> ent)
    {
        if (!TryComp<DamageableComponent>(ent.Owner, out var damageable) || _damageable.GetTotalDamage((ent.Owner, damageable)) <= 0)
            return;

        var scale = Math.Min(1f, ent.Comp.SelfRepair.Float() / _damageable.GetTotalDamage((ent.Owner, damageable)).Float());
        var heal = damageable.Damage * -scale;

        _damageable.TryChangeDamage(ent.Owner, heal, true);
    }
}
