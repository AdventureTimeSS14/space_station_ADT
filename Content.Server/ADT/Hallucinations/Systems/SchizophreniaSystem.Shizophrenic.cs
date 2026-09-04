using System.Linq;
using Content.Server.ADT.Chat;
using Content.Server.ADT.Hallucinations.Components;
using Content.Server.ADT.Hallucinations.Events;
using Content.Shared.ADT.Hallucinations.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects.Effects.StatusEffects;
using Content.Shared.Eye;
using Content.Shared.Mobs.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Hallucinations.Systems;

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
        SubscribeLocalEvent<HallucinationsRemoveMobsComponent, DamageDealtEvent>(OnDamage);
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
        AddOrAdjustHallucinations(ent.Owner, args.Id, args.Duration, args.OverwriteTimer ? StatusEffectMetabolismType.Set : StatusEffectMetabolismType.Add);
    }

    private void OnRemove(Entity<HallucinatingComponent> ent, ref RemoveHallucinationsEvent args)
    {
        AdjustAllHallucinations(ent.Owner, args.Time);
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

    private void OnDamage(Entity<HallucinationsRemoveMobsComponent> ent, ref DamageDealtEvent args)
    {
        if (!args.Origin.HasValue)
            return;

        if (!args.Damage.AnyPositive())
            return;

        if (string.IsNullOrEmpty(ent.Comp.Reveal))
            return;

        var reveal = Spawn(ent.Comp.Reveal, Transform(args.Origin.Value).Coordinates);
        AddAsHallucination(ent.Owner, reveal);
    }

    private void AddHallucinations(EntityUid uid, ProtoId<HallucinationsPackPrototype> pack, float duration, StatusEffectMetabolismType metabolism)
    {
        if (metabolism == StatusEffectMetabolismType.Remove)
            return;

        var comp = EnsureComp<HallucinatingComponent>(uid);

        // Get and add entry
        var packProto = _proto.Index(pack);
        var data = packProto.Data;

        HashSet<HallucinatingComponent.HallucinationCompound>? entries = new();
        if (data != null)
        {
            entries = new();
            foreach (var type in data)
            {
                entries.Add(new HallucinatingComponent.HallucinationCompound(type, _timing.CurTime));
            }
        }

        comp.Hallucinations.Add(pack, entries);

        EntityManager.AddComponents(uid, packProto.Components);

        if (!string.IsNullOrEmpty(packProto.StartingMessage))
            _popup.PopupEntity(Loc.GetString(packProto.StartingMessage), uid, uid, packProto.MessageType);

        // If not infinite, add timer
        if (duration > 0)
            comp.Removes.Add(pack, _timing.CurTime + TimeSpan.FromSeconds(duration));
    }

    private void AdjustHallucinations(EntityUid uid, ProtoId<HallucinationsPackPrototype> pack, float duration, StatusEffectMetabolismType metabolism)
    {
        var comp = EnsureComp<HallucinatingComponent>(uid);

        switch (metabolism)
        {
            case StatusEffectMetabolismType.Update:
                if (comp.Removes.TryGetValue(pack, out _))
                    comp.Removes[pack] = _timing.CurTime + TimeSpan.FromSeconds(duration);
                else
                    comp.Removes.Add(pack, _timing.CurTime + TimeSpan.FromSeconds(duration));

                break;
            case StatusEffectMetabolismType.Add:
                if (comp.Removes.TryGetValue(pack, out _))
                    comp.Removes[pack] += TimeSpan.FromSeconds(duration);
                else
                    comp.Removes.Add(pack, _timing.CurTime + TimeSpan.FromSeconds(duration));
                break;
            case StatusEffectMetabolismType.Set:
                if (comp.Removes.TryGetValue(pack, out _))
                    comp.Removes[pack] = _timing.CurTime + TimeSpan.FromSeconds(duration);
                else
                    comp.Removes.Add(pack, _timing.CurTime + TimeSpan.FromSeconds(duration));

                break;
            default:
                break;
        }
    }

    #region Public API
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
    public void AddOrAdjustHallucinations(EntityUid uid, ProtoId<HallucinationsPackPrototype> pack, float duration, StatusEffectMetabolismType type)
    {
        var comp = EnsureComp<HallucinatingComponent>(uid);

        if (comp.Hallucinations.Keys.Contains(pack))
            AdjustHallucinations(uid, pack, duration, type);
        else
            AddHallucinations(uid, pack, duration, type);
    }

    public void AdjustAllHallucinations(EntityUid uid, float duration)
    {
        var comp = EnsureComp<HallucinatingComponent>(uid);

        for (var i = 0; i < comp.Removes.Count; i++)
        {
            var item = comp.Removes.ElementAt(i);

            comp.Removes[item.Key] = item.Value + TimeSpan.FromSeconds(duration);
        }
    }
    #endregion
}
