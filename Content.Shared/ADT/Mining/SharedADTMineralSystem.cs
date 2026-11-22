using Content.Shared.ADT.Mining.Components;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Gatherable;
using Content.Shared.Gatherable.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.ADT.Mining;

public abstract class SharedADTMineralSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private GatherableSystem _gatherable = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMineralComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<ADTMineralComponent, ADTMineralDrillDoAfterEvent>(OnDrillDoAfter);
        SubscribeLocalEvent<ADTMineralComponent, ADTGatherAttemptEvent>(OnGatherAttempt);
    }

    private void OnAttacked(Entity<ADTMineralComponent> ent, ref AttackedEvent args)
    {
        OnHit(ent, args.User, args.User);

        if (args.Used == args.User)
        {
            if (HasComp<MegafaunaComponent>(args.User))
                Mine(ent.AsNullable(), 1, args.User);

            return;
        }

        if (!TryComp<GatherableComponent>(ent, out var gatherable))
            return;

        if (_whitelist.IsWhitelistFailOrNull(gatherable.ToolWhitelist, args.Used))
            return;

        var tool = CompOrNull<ADTMiningToolComponent>(args.Used);
        var speed = tool?.ToolSpeed ?? 1f;
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.MineTime * speed, new ADTMineralDrillDoAfterEvent(), ent.Owner, ent.Owner, args.Used)
        {
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _audio.PlayPredicted(tool?.UseSound, ent, args.User);
    }

    private void OnDrillDoAfter(Entity<ADTMineralComponent> ent, ref ADTMineralDrillDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        _audio.PlayPredicted(CompOrNull<ADTMiningToolComponent>(args.Used)?.UseSound, ent, args.User);
        _gatherable.Gather(ent.Owner, args.User);
    }

    private void OnGatherAttempt(Entity<ADTMineralComponent> ent, ref ADTGatherAttemptEvent args)
    {
        if (ent.Comp.Breaking)
            return;

        if (args.Gatherer is { } gatherer && HasComp<ProjectileComponent>(gatherer))
            OnHit(ent, gatherer, null);

        var power = CompOrNull<ADTDrillPowerComponent>(args.Gatherer)?.Power ?? 1;
        if (!Drill(ent, power, args.Gatherer, false))
            args.Cancelled = true;
    }

    public void Mine(Entity<ADTMineralComponent?> ent, int power, EntityUid? user, bool byExplosion = false)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Breaking || TerminatingOrDeleted(ent))
            return;

        if (!Drill((ent, ent.Comp), power, user, byExplosion))
            return;

        Destroy((ent, ent.Comp), user);
    }

    public void Destroy(Entity<ADTMineralComponent> ent, EntityUid? user)
    {
        ent.Comp.Breaking = true;
        Dirty(ent);
        _gatherable.Gather(ent.Owner, user);
    }

    private bool Drill(Entity<ADTMineralComponent> ent, int power, EntityUid? user, bool byExplosion)
    {
        if (power <= 0)
            return false;

        var ev = new ADTMineralDrillEvent(power, user, byExplosion);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Handled)
            return false;

        ent.Comp.Hardness -= power;
        Dirty(ent);

        if (ent.Comp.Hardness > 0)
        {
            UpdateCracks(ent);
            return false;
        }

        OnBroken(ent, user, byExplosion);
        return true;
    }

    private void UpdateCracks(Entity<ADTMineralComponent> ent)
    {
        var cracks = ADTMineralCracks.None;
        if (ent.Comp.Hardness < ent.Comp.MaxHardness)
        {
            cracks = ent.Comp.Hardness switch
            {
                1 => ADTMineralCracks.One,
                2 => ADTMineralCracks.Two,
                _ => ADTMineralCracks.None,
            };
        }

        _appearance.SetData(ent, ADTMineralVisuals.Cracks, cracks);
    }

    protected virtual void OnHit(Entity<ADTMineralComponent> ent, EntityUid source, EntityUid? predictedBy)
    {
    }

    protected virtual void OnBroken(Entity<ADTMineralComponent> ent, EntityUid? user, bool byExplosion)
    {
    }
}
