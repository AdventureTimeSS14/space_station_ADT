using Content.Shared.ADT.NightVision;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Examine;
using Content.Shared.Follower;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class ADTWispLanternSystem : EntitySystem
{
    [Dependency] private readonly FollowerSystem _follower = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedNightVisionSystem _nightVision = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private const int CarrySearchDepth = 5;

    public override void Initialize()
    {
        SubscribeLocalEvent<ADTWispLanternComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ADTWispLanternComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ADTWispLanternComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ADTWispLanternComponent, EntParentChangedMessage>(OnParentChanged);
    }

    private void OnParentChanged(Entity<ADTWispLanternComponent> ent, ref EntParentChangedMessage args)
    {
        if (!ent.Comp.Released)
            return;

        if (ent.Comp.User is { } user && !TerminatingOrDeleted(user) && IsCarriedBy(ent.Owner, user))
            return;

        Recall(ent);
    }

    private void OnShutdown(Entity<ADTWispLanternComponent> ent, ref ComponentShutdown args)
    {
        if (!ent.Comp.Released)
            return;

        Recall(ent);
    }

    private void OnExamined(Entity<ADTWispLanternComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.Released
            ? "adt-wisp-lantern-examine-released"
            : "adt-wisp-lantern-examine-stored"));
    }

    private void OnUseInHand(Entity<ADTWispLanternComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Released)
            Recall(ent);
        else
            Release(ent, args.User);
    }

    private void Release(Entity<ADTWispLanternComponent> ent, EntityUid user)
    {
        ent.Comp.Released = true;
        ent.Comp.User = user;

        var wisp = Spawn(ent.Comp.WispProto, Transform(user).Coordinates);
        _follower.StartFollowingEntity(wisp, user);
        ent.Comp.Wisp = wisp;

        if (!TryComp<NightVisionComponent>(user, out var nvComp))
        {
            EnsureComp<NightVisionComponent>(user);
            ent.Comp.GrantedVision = true;
            ent.Comp.WasVisionActive = false;
            ent.Comp.WasOverlay = false;
        }
        else
        {
            ent.Comp.GrantedVision = false;
            ent.Comp.WasVisionActive = nvComp.State != NightVisionState.Off;
            ent.Comp.WasOverlay = nvComp.Overlay;
        }

        _nightVision.SetActive(user, true, ent.Comp.WasOverlay);

        _light.SetRadius(ent.Owner, ent.Comp.ReleasedRadius);
        _appearance.SetData(ent.Owner, ADTWispLanternVisuals.Released, true);
        _audio.PlayPvs(ent.Comp.ReleaseSound, ent.Owner);

        _popup.PopupEntity(Loc.GetString("adt-wisp-lantern-release", ("lantern", ent.Owner)), user, user);
    }

    private void Recall(Entity<ADTWispLanternComponent> ent)
    {
        if (ent.Comp.Wisp is { } wisp && !TerminatingOrDeleted(wisp))
            QueueDel(wisp);

        ent.Comp.Wisp = null;

        if (ent.Comp.User is { } user && !TerminatingOrDeleted(user))
        {
            if (ent.Comp.GrantedVision)
            {
                _nightVision.SetActive(user, false, false);
                RemComp<NightVisionComponent>(user);
            }
            else
            {
                _nightVision.SetActive(user, ent.Comp.WasVisionActive, ent.Comp.WasOverlay);
            }

            _popup.PopupEntity(Loc.GetString("adt-wisp-lantern-return", ("lantern", ent.Owner)), user, user);
        }

        ent.Comp.GrantedVision = false;
        ent.Comp.WasVisionActive = false;
        ent.Comp.WasOverlay = false;
        ent.Comp.Released = false;
        ent.Comp.User = null;

        _light.SetRadius(ent.Owner, ent.Comp.StoredRadius);
        _appearance.SetData(ent.Owner, ADTWispLanternVisuals.Released, false);
        _audio.PlayPvs(ent.Comp.ReturnSound, ent.Owner);
    }

    private bool IsCarriedBy(EntityUid lantern, EntityUid user)
    {
        var parent = Transform(lantern).ParentUid;

        for (var depth = 0; depth < CarrySearchDepth && parent.IsValid(); depth++)
        {
            if (parent == user)
                return true;

            parent = Transform(parent).ParentUid;
        }

        return false;
    }
}
