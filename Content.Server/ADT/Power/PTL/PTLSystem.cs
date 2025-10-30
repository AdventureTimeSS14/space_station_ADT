using Content.Shared.Power.PTL;
using Content.Server.Flash;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.SMES;
using Content.Server.Stack;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Radiation.Components;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Numerics;
using System.Text;

namespace Content.Server.Power.PTL;

public sealed partial class PTLSystem : EntitySystem
{
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly AudioSystem _aud = default!;
    [Dependency] private readonly EmagSystem _emag = default!;

    [ValidatePrototypeId<StackPrototype>] private readonly string _stackCredits = "Credit";
    [ValidatePrototypeId<TagPrototype>] private readonly string _tagScrewdriver = "Screwdriver";
    [ValidatePrototypeId<TagPrototype>] private readonly string _tagMultitool = "Multitool";

    private readonly SoundPathSpecifier _soundKaching = new("/Audio/Effects/kaching.ogg");
    private readonly SoundPathSpecifier _soundSparks = new("/Audio/Effects/sparks4.ogg");
    private readonly SoundPathSpecifier _soundPower = new("/Audio/Effects/tesla_consume.ogg");

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(SmesSystem));
        SubscribeLocalEvent<PowerTransmissionLaserComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<PowerTransmissionLaserComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<PowerTransmissionLaserComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<PowerTransmissionLaserComponent, GotEmaggedEvent>(OnEmagged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var eqe = EntityQueryEnumerator<PowerTransmissionLaserComponent>();

        while (eqe.MoveNext(out var uid, out var ptl))
        {
            if (_time.CurTime > ptl.RadDecayTimer)
            {
                ptl.RadDecayTimer = _time.CurTime + TimeSpan.FromSeconds(1);
                DecayRad((uid, ptl));
            }

            if (!ptl.Active)
                continue;

            if (_time.CurTime > ptl.NextShotAt)
            {
                ptl.NextShotAt = _time.CurTime + TimeSpan.FromSeconds(ptl.ShootDelay);
                Tick((uid, ptl));
            }
        }
    }

    private void DecayRad(Entity<PowerTransmissionLaserComponent> ent)
    {
        if (TryComp<RadiationSourceComponent>(ent, out var rad)
            && rad.Intensity > 0)
            rad.Intensity -= rad.Intensity * 0.2f + .1f;
    }

    private void Tick(Entity<PowerTransmissionLaserComponent> ent)
    {
        if (!TryComp<BatteryComponent>(ent, out var battery)
        || battery.CurrentCharge < ent.Comp.MinShootPower)
            return;

        Shoot((ent, ent.Comp, battery));
        Dirty(ent);
    }

    private void Shoot(Entity<PowerTransmissionLaserComponent, BatteryComponent> ent)
    {
        var megajoule = 1e6;

        var charge = ent.Comp2.CurrentCharge / megajoule;
        // some random formula i found in bounty thread i popped it into desmos i think it looks good
        var spesos = (int) (charge * 500 / (Math.Log(charge * 5) + 1));

        if (charge <= 0 || !double.IsFinite(spesos) || spesos < 0)
            return;

        // scale damage from energy
        if (TryComp<HitscanBatteryAmmoProviderComponent>(ent, out var hitscan))
        {
            hitscan.FireCost = (float) (charge * megajoule);
            var prot = _protMan.Index<HitscanPrototype>(hitscan.Prototype);
            prot.Damage = ent.Comp1.BaseBeamDamage * charge * 2f;
        }

        if (TryComp<GunComponent>(ent, out var gun))
        {
            if (!TryComp<TransformComponent>(ent, out var xform))
                return;

            var localDirectionVector = Vector2.UnitY * -1;
            if (ent.Comp1.ReversedFiring)
                localDirectionVector *= -1f;

            var directionInParentSpace = xform.LocalRotation.RotateVec(localDirectionVector);

            var targetCoords = xform.Coordinates.Offset(directionInParentSpace);

            _gun.AttemptShoot(ent, ent, gun, targetCoords);
        }

        // EVIL behavior......
        var evil = (float) (charge * ent.Comp1.EvilMultiplier);

        if (TryComp<RadiationSourceComponent>(ent, out var rad))
            rad.Intensity = evil;

        _flash.FlashArea(ent, ent, evil/2, TimeSpan.FromMilliseconds(evil/2));

        ent.Comp1.SpesosHeld += spesos;
    }

    private void OnInteractHand(Entity<PowerTransmissionLaserComponent> ent, ref InteractHandEvent args)
    {
        ent.Comp.Active = !ent.Comp.Active;
        var enloc = ent.Comp.Active ? Loc.GetString("ptl-enabled") : Loc.GetString("ptl-disabled");
        var enabled = Loc.GetString("ptl-interact-enabled", ("enabled", enloc));
        _popup.PopupEntity(enabled, ent, Content.Shared.Popups.PopupType.SmallCaution);
        _aud.PlayPvs(_soundPower, args.User);

        Dirty(ent);
    }

    private void OnAfterInteractUsing(Entity<PowerTransmissionLaserComponent> ent, ref AfterInteractUsingEvent args)
    {
        var held = args.Used;

        if (_tag.HasTag(held, _tagScrewdriver))
        {
            var delay = ent.Comp.ShootDelay + ent.Comp.ShootDelayIncrement;
            if (delay > ent.Comp.ShootDelayThreshold.Max)
                delay = ent.Comp.ShootDelayThreshold.Min;
            ent.Comp.ShootDelay = delay;
            _popup.PopupEntity(Loc.GetString("ptl-interact-screwdriver", ("delay", ent.Comp.ShootDelay)), ent);
            _aud.PlayPvs(_soundSparks, args.User);
        }

        if (_tag.HasTag(held, _tagMultitool))
        {
            var stackPrototype = _protMan.Index<StackPrototype>(_stackCredits);
            _stack.Spawn((int) ent.Comp.SpesosHeld, stackPrototype, Transform(args.User).Coordinates);
            ent.Comp.SpesosHeld = 0;
            _popup.PopupEntity(Loc.GetString("ptl-interact-spesos"), ent);
            _aud.PlayPvs(_soundKaching, args.User);
        }

        Dirty(ent);
    }

    private void OnExamine(Entity<PowerTransmissionLaserComponent> ent, ref ExaminedEvent args)
    {
        var sb = new StringBuilder();
        var enloc = ent.Comp.Active ? Loc.GetString("ptl-enabled") : Loc.GetString("ptl-disabled");
        sb.AppendLine(Loc.GetString("ptl-examine-enabled", ("enabled", enloc)));
        sb.AppendLine(Loc.GetString("ptl-examine-spesos", ("spesos", ent.Comp.SpesosHeld)));
        sb.AppendLine(Loc.GetString("ptl-examine-screwdriver"));
        args.PushMarkup(sb.ToString());
    }

    private void OnEmagged(EntityUid uid, PowerTransmissionLaserComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        if (component.ReversedFiring)
            return;

        component.ReversedFiring = true;
        args.Handled = true;
    }
}
