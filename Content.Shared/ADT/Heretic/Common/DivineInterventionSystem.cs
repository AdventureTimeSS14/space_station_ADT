using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.ADT.Heretic.Common;

// ADT: from Goob Religion

/// <summary>
/// Handles "Spell Denial", these methods are largely targeted towards TargetActionEvents, however,
/// may also have other edge-cases.
/// </summary>
public sealed class DivineInterventionSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BeforeCastTouchSpellEvent>(OnTouchSpellAttempt);

        SubscribeLocalEvent<DivineInterventionComponent, TouchSpellDenialRelayEvent>(OnTouchSpellDenied);
    }

    private bool ShouldDeny(EntityUid target, out EntityUid? denyingItem)
    {
        denyingItem = null;
        var divineQuery = GetEntityQuery<DivineInterventionComponent>();

        foreach (var held in _hands.EnumerateHeld(target))
        {
            if (!divineQuery.HasComp(held))
                continue;

            denyingItem = held;
            return true;
        }

        var slots = _inventory.GetSlotEnumerator(target, SlotFlags.WITHOUT_POCKET);
        while (slots.NextItem(out var item, out var slot))
        {
            if (!divineQuery.TryComp(item, out var comp))
                continue;

            if ((slot.SlotFlags & comp.ValidSpellDenialSlots) == 0x0)
                continue;

            denyingItem = item;
            return true;
        }

        return false;
    }

    public bool ShouldDeny(EntityUid target) => ShouldDeny(target, out _);

    private void DenialEffects(EntityUid uid, EntityUid? entNullable, DivineInterventionComponent? comp = null)
    {
        if (_net.IsClient
            || entNullable is not { } ent
            || !Resolve(uid, ref comp))
            return;

        _popupSystem.PopupEntity(Loc.GetString(comp.DenialString), ent, PopupType.MediumCaution);
        _audio.PlayPvs(comp.DenialSound, ent);
        Spawn(comp.EffectProto, Transform(ent).Coordinates);
    }

    private void OnTouchSpellAttempt(BeforeCastTouchSpellEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (ShouldDeny(target, out var denyingItem)
            && denyingItem != null
            && Exists(denyingItem.Value))
        {
            args.Cancel();
            if (args.DoEffects)
                DenialEffects(denyingItem.Value, target);
        }
    }

    private void OnTouchSpellDenied(EntityUid uid, DivineInterventionComponent comp, TouchSpellDenialRelayEvent args)
    {
        var ev = new BeforeCastTouchSpellEvent(uid);
        RaiseLocalEvent(uid, ev, true);

        if (ev.Cancelled)
            args.Cancel();
    }

    public bool TouchSpellDenied(EntityUid uid)
    {
        var ev = new BeforeCastTouchSpellEvent(uid);
        RaiseLocalEvent(uid, ev, true);

        return ev.Cancelled;
    }
}
