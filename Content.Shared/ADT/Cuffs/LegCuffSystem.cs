using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Cuffs;

public sealed partial class LegCuffSystem : EntitySystem
{
    [Dependency] private SharedEnsnareableSystem _ensnareable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LegCuffComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<LegCuffComponent, LegCuffDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<LegCuffComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<LegCuffedComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<LegCuffedComponent, RemoveEnsnareAlertEvent>(OnSelfBreakoutAttempt);
    }

    private void OnAfterInteract(Entity<LegCuffComponent> ent, ref AfterInteractEvent args)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;

        if (args.Target is not { } target || !args.CanReach || args.Handled)
            return;

        if (!HasComp<MobStateComponent>(target))
            return;

        if (!TryComp<EnsnareableComponent>(target, out var ensnareableCheck))
            return;

        if (ensnareableCheck.IsEnsnared)
            return;

        _audio.PlayPredicted(comp.StartCuffSound, target, args.User);

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, comp.ApplyDelay,
            new LegCuffDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnDamage = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfter(Entity<LegCuffComponent> ent, ref LegCuffDoAfterEvent args)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;

        if (args.Cancelled || args.Handled || args.Args.Target is not { } target)
            return;

        if (!TryComp<EnsnaringComponent>(uid, out var ensnaring))
            return;

        if (!_ensnareable.TryEnsnare(target, uid, ensnaring))
        {
            args.Handled = true;
            return;
        }

        var cuffed = EnsureComp<LegCuffedComponent>(target);
        cuffed.CuffedSprite = comp.CuffedSprite;
        cuffed.NextAllowedTime = TimeSpan.Zero;
        Dirty(target, cuffed);

        args.Handled = true;
    }

    private void OnGetVerbs(Entity<LegCuffedComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        var uid = ent.Owner;

        if (args.User == uid)
            return;

        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<EnsnareableComponent>(uid, out var ensnareable) || !ensnareable.IsEnsnared)
            return;

        EntityUid? legCuffEntity = null;

        foreach (var contained in ensnareable.Container.ContainedEntities)
        {
            if (!HasComp<LegCuffComponent>(contained))
                continue;

            legCuffEntity = contained;
            break;
        }

        if (legCuffEntity == null)
            return;

        var user = args.User;

        var verb = new InteractionVerb
        {
            Text = Loc.GetString("legcuffs-verb-remove"),
            Icon = new SpriteSpecifier.Texture(
                new ResPath("/Textures/Interface/VerbIcons/unlock.svg.192dpi.png")),
            Act = () =>
            {
                if (!TryComp<EnsnaringComponent>(legCuffEntity, out var ensnaring))
                    return;

                if (TryComp<LegCuffComponent>(legCuffEntity, out var legCuff))
                    _audio.PlayPredicted(legCuff.RemoveCuffSound, uid, user);

                _ensnareable.TryFree(uid, user, legCuffEntity.Value, ensnaring);
            }
        };

        args.Verbs.Add(verb);
    }

    private void OnSelfBreakoutAttempt(Entity<LegCuffedComponent> ent, ref RemoveEnsnareAlertEvent args)
    {
        var uid = ent.Owner;

        if (!TryComp<EnsnareableComponent>(uid, out var ensnareable) || !ensnareable.IsEnsnared)
            return;

        var now = _timing.CurTime;

        if (now < ent.Comp.NextAllowedTime)
            return;

        ent.Comp.NextAllowedTime = now + ent.Comp.BreakoutSoundCooldown;

        foreach (var contained in ensnareable.Container.ContainedEntities)
        {
            if (!TryComp<LegCuffComponent>(contained, out var legCuff))
                continue;

            _audio.PlayPredicted(legCuff.RemoveCuffSound, uid, uid);
            break;
        }
    }

    private void OnRemovedFromContainer(Entity<LegCuffComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        var victim = args.Container.Owner;

        if (!TryComp<EnsnareableComponent>(victim, out var ens))
            return;

        if (!ReferenceEquals(ens.Container, args.Container))
            return;

        RemCompDeferred<LegCuffedComponent>(victim);
    }
}
