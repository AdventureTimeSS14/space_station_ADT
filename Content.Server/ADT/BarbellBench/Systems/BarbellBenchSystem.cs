using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared.ADT.BarbellBench;
using Content.Shared.ADT.BarbellBench.Components;
using Content.Shared.ADT.BarbellBench.Systems;
using Content.Shared.Audio;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server.ADT.BarbellBench.Systems;

public sealed class BarbellBenchSystem : SharedBarbellBenchSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarbellBenchComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BarbellBenchComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BarbellBenchComponent, BarbellBenchPerformRepEvent>(OnPerformRep);
        SubscribeLocalEvent<BarbellBenchComponent, AttachableHolderAttachablesAlteredEvent>(OnAttachableAltered);
        SubscribeLocalEvent<BarbellLiftComponent, UseInHandEvent>(OnBarbellUseInHand);
    }

    private void OnAttachableAltered(EntityUid uid, BarbellBenchComponent component, ref AttachableHolderAttachablesAlteredEvent args)
    {
        if (args.SlotId != component.BarbellSlotId)
            return;

        if (component.OverlayEntity is not { } overlay || !Exists(overlay))
            return;

        _appearance.SetData(uid, BarbellBenchVisuals.HasBarbell, args.Alteration == AttachableAlteredType.Attached);

        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                var barbellMeta = MetaData(args.Attachable);
                _metaData.SetEntityName(overlay, barbellMeta.EntityName);
                _metaData.SetEntityDescription(overlay, barbellMeta.EntityDescription);
                break;

            case AttachableAlteredType.Detached:
                var overlayMeta = MetaData(overlay);
                if (overlayMeta.EntityPrototype != null)
                {
                    _metaData.SetEntityName(overlay, overlayMeta.EntityPrototype.Name);
                    _metaData.SetEntityDescription(overlay, overlayMeta.EntityPrototype.Description);
                }
                break;
        }
    }

    private void OnBarbellUseInHand(Entity<BarbellLiftComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var msg = Loc.GetString(ent.Comp.EmoteLoc, ("name", Name(args.User)));
        _chat.TrySendInGameICMessage(args.User, msg, InGameICChatType.Emote, ChatTransmitRange.Normal,
            ignoreActionBlocker: true, nameOverride: string.Empty);

        var selfMsg = Loc.GetString(ent.Comp.EmoteLocSelf);
        _popup.PopupEntity(selfMsg, args.User, args.User, PopupType.Medium);

        _stamina.TakeStaminaDamage(args.User, ent.Comp.StaminaCost, source: args.User, with: ent.Owner, visual: true);

        args.Handled = true;
    }

    private void OnStartup(EntityUid uid, BarbellBenchComponent component, ComponentStartup args)
    {
        EnsureOverlay(uid, component);
        UpdateAppearance(uid, component);
    }

    private void OnShutdown(EntityUid uid, BarbellBenchComponent component, ComponentShutdown args)
    {
        if (component.OverlayEntity is { } overlay && Exists(overlay))
            Del(overlay);
        component.OverlayEntity = null;
    }

    private void EnsureOverlay(EntityUid uid, BarbellBenchComponent component)
    {
        if (component.OverlayEntity is { } existing && Exists(existing))
            return;

        var coords = Transform(uid).Coordinates;
        var overlay = Spawn(component.OverlayPrototype, coords);

        var overlayXform = Transform(overlay);
        _transform.SetParent(overlay, overlayXform, uid);
        _transform.SetCoordinates(overlay, overlayXform, new EntityCoordinates(uid, Vector2.Zero));
        overlayXform.LocalRotation = Angle.Zero;

        component.OverlayEntity = overlay;
        Dirty(uid, component);
    }

    private void OnPerformRep(EntityUid uid, BarbellBenchComponent component, BarbellBenchPerformRepEvent args)
    {
        if (component.IsPerformingRep)
            return;

        if (_container.TryGetContainer(uid, component.BarbellSlotId, out var barbellContainer) && barbellContainer.Count > 0)
        {
            var barbell = barbellContainer.ContainedEntities[0];
            if (TryComp<BarbellLiftComponent>(barbell, out var lift))
            {
                _stamina.TakeStaminaDamage(args.Performer, lift.StaminaCost, source: args.Performer, with: barbell, visual: true);
                _popup.PopupEntity(Loc.GetString(lift.EmoteLocSelf), args.Performer, args.Performer, PopupType.Medium);

                PullerComponent? pullerComp = null;
                if (Resolve(args.Performer, ref pullerComp))
                {
                    pullerComp.PulledDensityReduction = Math.Min(pullerComp.PulledDensityReduction + 0.05f, 0.8f);
                    Dirty(args.Performer, pullerComp);
                }
            }
        }

        component.IsPerformingRep = true;
        Dirty(uid, component);
        UpdateAppearance(uid, component);

        var sound = new SoundCollectionSpecifier(component.RepSoundCollection);
        Timer.Spawn(TimeSpan.FromSeconds(component.RepSoundDelay), () =>
        {
            if (Exists(uid))
                _audio.PlayPvs(sound, uid);
        });

        Timer.Spawn(TimeSpan.FromSeconds(component.RepDuration), () =>
        {
            if (!TryComp<BarbellBenchComponent>(uid, out var comp))
                return;

            comp.IsPerformingRep = false;
            Dirty(uid, comp);
            UpdateAppearance(uid, comp);
        });

        args.Handled = true;
    }

}
