using Content.Server.ADT.Chat;
using Content.Shared.ADT.Shizophrenia;
using Content.Shared.Damage;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Eye;
using Content.Shared.Humanoid;
using Content.Shared.Kitchen;
using Content.Shared.Mobs.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Shizophrenia;

public sealed partial class SchizophreniaSystem : EntitySystem
{
    private void InitializeShizophrenic()
    {
        SubscribeLocalEvent<SchizophreniaComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SchizophreniaComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<CanHallucinateComponent, AddHallucinationsEvent>(OnAddMobs);
        SubscribeLocalEvent<HallucinatingComponent, RemoveHallucinationsEvent>(OnRemove);

        SubscribeLocalEvent<HallucinationsRemoveMobsComponent, ComponentStartup>(OnRemoveMobsStartup);
        SubscribeLocalEvent<HallucinationsRemoveMobsComponent, CanHearVoiceEvent>(OnCanHearVoice);
        SubscribeLocalEvent<HallucinationsRemoveMobsComponent, CanReceiveChatMessageEvent>(OnCanReceiveMessage);
        SubscribeLocalEvent<HallucinationsRemoveMobsComponent, DamageChangedEvent>(OnDamage);
    }

    private void OnPlayerAttached(Entity<SchizophreniaComponent> ent, ref PlayerAttachedEvent args)
    {
        foreach (var item in ent.Comp.Hallucinations)
            _pvsOverride.AddForceSend(item, args.Player);
    }

    private void OnPlayerDetached(Entity<SchizophreniaComponent> ent, ref PlayerDetachedEvent args)
    {
        foreach (var item in ent.Comp.Hallucinations)
            _pvsOverride.RemoveForceSend(item, args.Player);
    }

    private void OnAddMobs(Entity<CanHallucinateComponent> ent, ref AddHallucinationsEvent args)
    {
        AddOrAdjustHallucinations(ent.Owner, args.Id, args.Duration, args.OverwriteTimer ? HallucinationsMetabolismType.Set : HallucinationsMetabolismType.Add);
    }

    private void OnRemove(Entity<HallucinatingComponent> ent, ref RemoveHallucinationsEvent args)
    {
        if (args.Time.HasValue)
        {
            AddOrAdjustHallucinations(ent.Owner, args.Id, args.Time.Value, HallucinationsMetabolismType.Remove);
            return;
        }

        if (ent.Comp.Hallucinations.ContainsKey(args.Id))
            ent.Comp.Hallucinations.Remove(args.Id);

        if (ent.Comp.Removes.ContainsKey(args.Id))
            ent.Comp.Removes.Remove(args.Id);

        EntityManager.RemoveComponents(ent.Owner, _proto.Index(args.Id).Components);
    }

    private void OnRemoveMobsStartup(Entity<HallucinationsRemoveMobsComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.StartingMessage != "")
            _popup.PopupEntity(Loc.GetString(ent.Comp.StartingMessage), ent.Owner, ent.Owner, Shared.Popups.PopupType.MediumCaution);
    }

    private void OnCanHearVoice(Entity<HallucinationsRemoveMobsComponent> ent, ref CanHearVoiceEvent args)
    {
        if (args.Source == ent.Owner)
            return;

        if (HasComp<MobStateComponent>(args.Source) && !HasComp<HallucinationComponent>(args.Source))
            args.Cancelled = true;
    }

    private void OnCanReceiveMessage(Entity<HallucinationsRemoveMobsComponent> ent, ref CanReceiveChatMessageEvent args)
    {
        if (args.Source == ent.Owner)
            return;

        if (HasComp<MobStateComponent>(args.Source) && !HasComp<HallucinationComponent>(args.Source))
            args.Cancelled = true;
    }

    private void OnDamage(Entity<HallucinationsRemoveMobsComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.Origin.HasValue)
            return;

        if (!args.DamageIncreased)
            return;

        if (string.IsNullOrEmpty(ent.Comp.Reveal))
            return;

        var reveal = Spawn(ent.Comp.Reveal, Transform(args.Origin.Value).Coordinates);
        AddAsHallucination(ent.Owner, reveal);
    }

    #region Public
    /// <summary>
    /// Makes entity a hallucination for another one
    /// </summary>
    /// <param name="uid">Hallucinating entity</param>
    /// <param name="toAdd">Hallucination</param>
    /// <param name="dirty">Whether dirty comps or not. Used for sounds and pointers that does not have to be networked</param>
    public void AddAsHallucination(EntityUid uid, EntityUid toAdd, bool dirty = true)
    {
        var comp = EnsureComp<SchizophreniaComponent>(uid);

        // Set invisible (kinda) layer
        _visibility.SetLayer(toAdd, (ushort) VisibilityFlags.Hallucination, true);

        // Add pvs override if can
        if (_player.TryGetSessionByEntity(uid, out var session))
            _pvsOverride.AddForceSend(toAdd, session);

        comp.Hallucinations.Add(toAdd);

        // Just needed, else game crashes
        var hallucination = new HallucinationComponent()
        {
            Ent = uid
        };
        AddComp(toAdd, hallucination);

        // We dont need to change index if entity is already hallucinating
        if (comp.Idx <= 0)
        {
            comp.Idx = _nextIdx;
            _nextIdx++;
        }

        hallucination.Idx = comp.Idx;

        // Dirty if needed
        if (dirty)
        {
            Dirty(uid, comp);
            Dirty(toAdd, hallucination);
        }
    }

    /// <summary>
    /// Applies a certain hallucination pack to the entity
    /// </summary>
    /// <param name="uid">Target entity</param>
    /// <param name="pack">Hallucinations pack</param>
    /// <param name="duration">Duration of the effect or removed time</param>
    /// <param name="type">Add/Set/Remove</param>
    public void AddOrAdjustHallucinations(EntityUid uid, ProtoId<HallucinationsPackPrototype> pack, float duration, HallucinationsMetabolismType type)
    {
        var comp = EnsureComp<HallucinatingComponent>(uid);

        switch (type)
        {
            case HallucinationsMetabolismType.Add:
                if (!comp.Hallucinations.ContainsKey(pack))
                    break;

                if (!comp.Removes.ContainsKey(pack))
                    return;

                comp.Removes[pack] += TimeSpan.FromSeconds(duration);
                return;
            case HallucinationsMetabolismType.Set:
                if (!comp.Hallucinations.ContainsKey(pack))
                    break;

                comp.Removes[pack] = _timing.CurTime + TimeSpan.FromSeconds(duration);
                return;
            case HallucinationsMetabolismType.Remove:
                if (!comp.Hallucinations.ContainsKey(pack))
                    return;

                if (!comp.Removes.ContainsKey(pack) && duration > 0)
                    return;

                if (duration <= 0)
                    comp.Removes[pack] = _timing.CurTime;
                else
                    comp.Removes[pack] -= TimeSpan.FromSeconds(duration);
                return;
            default:
                break;
        }

        // Get and add entry
        var packProto = _proto.Index(pack);
        var data = packProto.Data;

        if (data != null)
        {
            var entry = data.GetEntry();
            comp.Hallucinations.Add(pack, entry);
        }
        else
            comp.Hallucinations.Add(pack, null);

        EntityManager.AddComponents(uid, packProto.Components);

        EntityManager.AddComponents(uid, _proto.Index(pack).Components);

        // If not infinite, add timer
        if (duration > 0)
            comp.Removes.Add(pack, _timing.CurTime + TimeSpan.FromSeconds(duration));
    }
    #endregion
}
