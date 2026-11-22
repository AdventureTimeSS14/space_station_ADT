using Content.Shared.ADT.ModSuits;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.ADT.ModSuits;

public sealed partial class ModSuitEnergyShieldSystem : EntitySystem
{
    public const string ShieldBubblePrototype = "ADTModsuitShieldBubble";

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private MetaDataSystem _meta = default!;

    private readonly HashSet<EntityUid> _recharging = new();
    private readonly List<EntityUid> _finished = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModSuitEnergyShieldStateComponent, ComponentStartup>(OnStateStartup);
        SubscribeLocalEvent<ModSuitEnergyShieldStateComponent, ComponentShutdown>(OnStateShutdown);

        SubscribeLocalEvent<ModSuitEnergyShieldComponent, ComponentStartup>(OnShieldStartup);
        SubscribeLocalEvent<ModSuitEnergyShieldComponent, ComponentShutdown>(OnShieldShutdown);
        SubscribeLocalEvent<ModSuitEnergyShieldComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);

        SubscribeLocalEvent<ModSuitComponent, ClothingGotEquippedEvent>(OnSuitEquipped);
        SubscribeLocalEvent<ModSuitComponent, ClothingGotUnequippedEvent>(OnSuitUnequipped);
    }

    private void OnStateStartup(Entity<ModSuitEnergyShieldStateComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Health == 0f)
            _recharging.Add(ent.Owner);
    }

    private void OnStateShutdown(Entity<ModSuitEnergyShieldStateComponent> ent, ref ComponentShutdown args)
    {
        _recharging.Remove(ent.Owner);
    }

    private void OnShieldStartup(Entity<ModSuitEnergyShieldComponent> ent, ref ComponentStartup args)
    {
        if (TryGetState(ent.Owner, out var state))
            RefreshShield(ent.Owner, ent.Comp, state.Comp);
    }

    private void OnShieldShutdown(Entity<ModSuitEnergyShieldComponent> ent, ref ComponentShutdown args)
    {
        RemoveBubble(ent.Comp);
    }

    private void OnSuitEquipped(Entity<ModSuitComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!TryComp<ModSuitEnergyShieldComponent>(ent, out var shield) || !TryGetState(ent.Owner, out var state))
            return;

        if ((state.Comp.Health ?? state.Comp.MaxHealth) <= 0f)
            return;

        EnsureBubble(shield, args.Wearer);
    }

    private void OnSuitUnequipped(Entity<ModSuitComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (TryComp<ModSuitEnergyShieldComponent>(ent, out var shield))
            RemoveBubble(shield);
    }

    private void OnDamageModify(Entity<ModSuitEnergyShieldComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        var damage = args.Args.Damage;

        if (damage.Empty || !damage.AnyPositive() || args.Args.Origin == null)
            return;

        if (!TryComp<ModSuitComponent>(ent, out var suit) || suit.TempUser is not { } wearer)
            return;

        if (!TryGetState(ent.Owner, out var state))
            return;

        var current = state.Comp.Health ?? state.Comp.MaxHealth;

        if (current <= 0f)
            return;

        args.Args.Damage = new DamageSpecifier();

        var health = Math.Max(0f, current - ent.Comp.DamagePerBlock);
        state.Comp.Health = health;
        ent.Comp.CurrentHealth = health;
        Dirty(ent.Owner, ent.Comp);

        SpawnAtPosition("EffectSparks", Transform(wearer).Coordinates);

        if (health > 0f)
            return;

        state.Comp.NextRechargeTime = _timing.CurTime + state.Comp.RechargeTime;
        _recharging.Add(state.Owner);

        _popup.PopupEntity(Loc.GetString("modsuit-energy-shield-overload"), wearer, wearer, PopupType.Medium);
        RemoveBubble(ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_recharging.Count == 0)
            return;

        var curTime = _timing.CurTime;

        foreach (var uid in _recharging)
        {
            if (!TryComp(uid, out ModSuitEnergyShieldStateComponent? state))
            {
                _finished.Add(uid);
                continue;
            }

            if (state.Health is not { } health || health > 0f)
            {
                state.NextRechargeTime = null;
                _finished.Add(uid);
                continue;
            }

            var pauseTime = _meta.GetPauseTime(uid);
            state.NextRechargeTime ??= curTime - pauseTime + state.RechargeTime;

            if (curTime <= state.NextRechargeTime.Value + pauseTime)
                continue;

            state.Health = state.MaxHealth;
            state.NextRechargeTime = null;
            _finished.Add(uid);

            if (_container.TryGetContainingContainer(uid, out var container)
                && TryComp<ModSuitEnergyShieldComponent>(container.Owner, out var shield))
            {
                RefreshShield(container.Owner, shield, state);
            }
        }

        foreach (var uid in _finished)
        {
            _recharging.Remove(uid);
        }

        _finished.Clear();
    }

    private void RefreshShield(EntityUid uid, ModSuitEnergyShieldComponent shield, ModSuitEnergyShieldStateComponent state)
    {
        var max = state.MaxHealth;
        var health = Math.Clamp(state.Health ?? max, 0f, max);
        var recharged = shield.MaxHealth > 0f && shield.CurrentHealth <= 0f && health > 0f;

        if (shield.CurrentHealth != health || shield.MaxHealth != max)
        {
            shield.CurrentHealth = health;
            shield.MaxHealth = max;
            Dirty(uid, shield);
        }

        if (!TryComp<ModSuitComponent>(uid, out var suit) || suit.TempUser is not { } wearer || !Exists(wearer))
        {
            RemoveBubble(shield);
            return;
        }

        if (recharged)
            _popup.PopupEntity(Loc.GetString("modsuit-energy-shield-recharged"), wearer, wearer, PopupType.Medium);

        if (health <= 0f)
        {
            RemoveBubble(shield);
            return;
        }

        EnsureBubble(shield, wearer);
    }

    private bool TryGetState(EntityUid suit, out Entity<ModSuitEnergyShieldStateComponent> state)
    {
        state = default;

        if (!TryComp<ModSuitComponent>(suit, out var modSuit))
            return false;

        foreach (var moduleUid in modSuit.ModuleContainer.ContainedEntities)
        {
            if (TryComp<ModSuitEnergyShieldStateComponent>(moduleUid, out var shieldState))
            {
                state = (moduleUid, shieldState);
                return true;
            }
        }

        return false;
    }

    private void EnsureBubble(ModSuitEnergyShieldComponent shield, EntityUid wearer)
    {
        if (shield.Bubble is { } bubble && !TerminatingOrDeleted(bubble))
        {
            if (shield.Wearer == wearer)
                return;

            _transform.SetParent(bubble, wearer);
            shield.Wearer = wearer;
            return;
        }

        bubble = SpawnAtPosition(ShieldBubblePrototype, Transform(wearer).Coordinates);
        _transform.SetParent(bubble, wearer);
        _transform.SetLocalRotation(bubble, Angle.Zero);

        shield.Bubble = bubble;
        shield.Wearer = wearer;
    }

    private void RemoveBubble(ModSuitEnergyShieldComponent shield)
    {
        if (shield.Bubble is { } bubble && !TerminatingOrDeleted(bubble))
            QueueDel(bubble);

        shield.Bubble = null;
        shield.Wearer = null;
    }
}
