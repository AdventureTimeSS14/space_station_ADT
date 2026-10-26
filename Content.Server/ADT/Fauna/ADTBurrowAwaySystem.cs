using Content.Shared.ADT.Fauna.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Fauna;

public sealed class ADTBurrowAwaySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<HumanoidProfileComponent>> _nearby = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTBurrowAwayComponent, DamageDealtEvent>(OnDamaged);
    }

    private void OnDamaged(Entity<ADTBurrowAwayComponent> ent, ref DamageDealtEvent args)
    {
        if (args.Damage.AnyPositive())
            Spook(ent);
    }

    public void Spook(Entity<ADTBurrowAwayComponent> ent)
    {
        if (ent.Comp.BurrowAt != null)
            return;

        ent.Comp.BurrowAt = _timing.CurTime + ent.Comp.ChaseTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTBurrowAwayComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobState.IsDead(uid))
                continue;

            if (comp.BurrowAt is { } burrowAt)
            {
                if (now >= burrowAt)
                    Burrow((uid, comp));

                continue;
            }

            if (now < comp.NextScan)
                continue;

            comp.NextScan = now + comp.ScanInterval;

            if (IsAnyoneNear((uid, comp)))
                Spook((uid, comp));
        }
    }

    private bool IsAnyoneNear(Entity<ADTBurrowAwayComponent> ent)
    {
        _nearby.Clear();
        _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(ent.Owner), ent.Comp.TriggerRange, _nearby);

        foreach (var person in _nearby)
        {
            if (!_mobState.IsDead(person.Owner))
                return true;
        }

        return false;
    }

    private void Burrow(Entity<ADTBurrowAwayComponent> ent)
    {
        ent.Comp.BurrowAt = null;

        _popup.PopupEntity(Loc.GetString(ent.Comp.Popup, ("creature", ToPrettyString(ent.Owner))), ent);
        _audio.PlayPvs(ent.Comp.Sound, ent);
        QueueDel(ent.Owner);
    }
}
