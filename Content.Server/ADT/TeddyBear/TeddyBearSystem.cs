using Content.Server.Explosion.EntitySystems;
using Content.Server.Hands.Systems;
using Content.Shared.ADT.TeddyBear;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.TeddyBear;

public sealed class TeddyBearSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TeddyBearComponent, TeddyBearSpawnGunEvent>(OnSpawnGun);
        SubscribeLocalEvent<TeddyBearComponent, TeddyBearExplodeEvent>(OnExplode);
    }

    private void OnSpawnGun(Entity<TeddyBearComponent> ent, ref TeddyBearSpawnGunEvent args)
    {
        if (args.Handled)
            return;

        var weapon = Spawn(ent.Comp.WeaponPrototype, Transform(ent).Coordinates);
        if (_hands.TryForcePickupAnyHand(ent, weapon))
        {
            _popup.PopupEntity(Loc.GetString("teddy-bear-gun-spawned"), ent, ent);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/screwdriver2.ogg"), ent);
        }
        else
        {
            QueueDel(weapon);
            _popup.PopupEntity(Loc.GetString("teddy-bear-gun-failed"), ent, ent);
        }
        args.Handled = true;
    }

    private void OnExplode(Entity<TeddyBearComponent> ent, ref TeddyBearExplodeEvent args)
    {
        if (args.Handled || ent.Comp.ExplodeTime != null)
            return;
        var beepSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg", AudioParams.Default.WithVolume(-5f));
        _audio.PlayPvs(beepSound, ent);
        ent.Comp.ExplodeTime = _timing.CurTime + TimeSpan.FromSeconds(2.5);
        ent.Comp.BeepCount = 0;
        ent.Comp.NextBeepTime = _timing.CurTime + TimeSpan.FromSeconds(0.7);
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TeddyBearComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ExplodeTime == null)
                continue;

            var curTime = _timing.CurTime;

            if (comp.BeepCount < 3 && curTime >= comp.NextBeepTime)
            {
                var beepSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg", AudioParams.Default.WithVolume(-5f));
                _audio.PlayPvs(beepSound, uid);
                comp.BeepCount++;
                comp.NextBeepTime = curTime + TimeSpan.FromSeconds(0.7);
            }

            if (curTime >= comp.ExplodeTime.Value)
            {
                _explosion.QueueExplosion(uid, "Default", 120f, 3f, 10f, canCreateVacuum: false);
                QueueDel(uid);
            }
        }
    }
}
