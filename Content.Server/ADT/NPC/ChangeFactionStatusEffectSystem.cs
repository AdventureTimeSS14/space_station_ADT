using Content.Server.NPC.HTN;
using Content.Server.NPC.Components;
using Content.Shared.CombatMode;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC;

public sealed partial class ChangeFactionStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly NpcFactionSystem _npc = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;

    public static readonly EntProtoId ChangeFactionStatusEffect = "ChangeFactionStatusEffect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeFactionStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusApplied);
        SubscribeLocalEvent<ChangeFactionStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusRemoved);
    }

    public void TryChangeFaction(EntityUid uid, ProtoId<NpcFactionPrototype> newFaction, out EntityUid? statusEffect, float durationInSeconds)
    {
        statusEffect = null;

        if (TryComp<MindContainerComponent>(uid, out var mindContainer) && mindContainer.HasMind)
            return;

        var npc = EnsureComp<NpcFactionMemberComponent>(uid);

        if (durationInSeconds <= 0f)
        {
            SwapFactions((uid, npc), newFaction);
            return;
        }

        if (!_status.TryAddStatusEffect(uid, ChangeFactionStatusEffect, out statusEffect, TimeSpan.FromSeconds(durationInSeconds)))
            return;

        if (statusEffect.HasValue && TryComp<ChangeFactionStatusEffectComponent>(statusEffect, out var f))
        {
            f.NewFaction = newFaction;
            var appliedEvent = new StatusEffectAppliedEvent(uid);
            OnStatusApplied((statusEffect.Value, f), ref appliedEvent);
        }
    }

    private void OnStatusApplied(Entity<ChangeFactionStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!ent.Comp.NewFaction.HasValue)
            return;

        var target = args.Target;
        var npc = EnsureComp<NpcFactionMemberComponent>(target);

        ent.Comp.OldFactions = new HashSet<ProtoId<NpcFactionPrototype>>(npc.Factions);
        SwapFactions((target, npc), ent.Comp.NewFaction.Value);
        SetupCombat(target, ent);
    }

    private void OnStatusRemoved(Entity<ChangeFactionStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        var target = args.Target;
        var npc = EnsureComp<NpcFactionMemberComponent>(target);

        SwapFactions((target, npc), ent.Comp.OldFactions);
        RestoreCombat(target, ent);
    }

    private void SetupCombat(EntityUid target, Entity<ChangeFactionStatusEffectComponent> ent)
    {
        if (TryComp<HTNComponent>(target, out var htn))
        {
            ent.Comp.OriginalRootTask = htn.RootTask;
            htn.RootTask = new HTNCompoundTask
            {
                Task = "SimpleHostileCompound"
            };
            htn.PlanAccumulator = htn.PlanCooldown;
        }
        else
        {
            var newHtn = AddComp<HTNComponent>(target);
            newHtn.RootTask = new HTNCompoundTask
            {
                Task = "SimpleHostileCompound"
            };
            ent.Comp.WasGivenHTN = true;
        }

        if (TryComp<CombatModeComponent>(target, out var combat))
        {
            _combatMode.SetInCombatMode(target, true, combat);
        }
        else
        {
            AddComp<CombatModeComponent>(target);
            _combatMode.SetInCombatMode(target, true);
            ent.Comp.WasGivenCombatMode = true;
        }

    }

    private void RestoreCombat(EntityUid target, Entity<ChangeFactionStatusEffectComponent> ent)
    {
        if (ent.Comp.OriginalRootTask != null && TryComp<HTNComponent>(target, out var htn))
        {
            htn.RootTask = ent.Comp.OriginalRootTask;
            htn.PlanAccumulator = htn.PlanCooldown;
        }
        else if (ent.Comp.WasGivenHTN)
        {
            RemComp<HTNComponent>(target);
        }

        if (ent.Comp.WasGivenCombatMode)
            RemComp<CombatModeComponent>(target);
    }

    private void SwapFactions(Entity<NpcFactionMemberComponent> ent, ProtoId<NpcFactionPrototype> faction)
    {
        _npc.ClearFactions((ent, ent.Comp));
        _npc.AddFaction((ent, ent.Comp), faction);
    }

    private void SwapFactions(Entity<NpcFactionMemberComponent> ent, HashSet<ProtoId<NpcFactionPrototype>> factions)
    {
        _npc.ClearFactions((ent, ent.Comp));
        if (factions.Count > 0)
            _npc.AddFactions((ent, ent.Comp), factions);
    }
}
