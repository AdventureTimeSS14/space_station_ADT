using Content.Server.ADT.Economy;
using Content.Server.Chat.Managers;
using Content.Shared.ADT.Economy;
using Content.Shared.ADT.Rituals;
using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Systems;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Rituals;

public sealed class ADTEmpathSystem : EntitySystem
{
    [Dependency] private readonly BankCardSystem _bank = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly string[] Thoughts =
    {
        "adt-empath-thought-snack",
        "adt-empath-thought-future",
        "adt-empath-thought-past",
        "adt-empath-thought-money",
        "adt-empath-thought-hair",
        "adt-empath-thought-plans",
        "adt-empath-thought-work",
        "adt-empath-thought-space",
        "adt-empath-thought-funny",
        "adt-empath-thought-sad",
        "adt-empath-thought-annoying",
        "adt-empath-thought-happy",
        "adt-empath-thought-nonsense",
        "adt-empath-thought-mistakes",
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTEmpathComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTEmpathComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ADTEmpathComponent, ADTEmpathActionEvent>(OnEmpathAction);
        SubscribeLocalEvent<MobStateComponent, AttackedEvent>(OnAttacked);
    }

    private void OnMapInit(Entity<ADTEmpathComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnShutdown(Entity<ADTEmpathComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnAttacked(Entity<MobStateComponent> ent, ref AttackedEvent args)
    {
        if (args.User == ent.Owner)
            return;

        var memory = EnsureComp<ADTEmpathMemoryComponent>(args.User);

        memory.LastTarget = ent.Owner;
        memory.LastAttack = _timing.CurTime;
    }

    private void OnEmpathAction(Entity<ADTEmpathComponent> ent, ref ADTEmpathActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryComp<ActorComponent>(ent.Owner, out var actor))
            return;

        if (!HasComp<MobStateComponent>(target) || !HasComp<MindContainerComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("adt-empath-no-mind"), ent.Owner, ent.Owner);
            return;
        }

        if (_mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("adt-empath-dead"), ent.Owner, ent.Owner);
            return;
        }

        args.Handled = true;

        Send(actor, Loc.GetString("adt-empath-header", ("target", target)));

        var (condition, thought) = ReadCondition(target);

        Send(actor, condition);
        Send(actor, ReadMood(target));

        if (ReadTarget(target) is { } aim)
            Send(actor, aim);

        if (ReadObjectives(target) is { } plans)
            Send(actor, plans);

        if (ReadNumbers(target) is { } numbers)
            Send(actor, numbers);

        Send(actor, Loc.GetString("adt-empath-thoughts", ("target", target), ("thought", Loc.GetString(thought))));

        _popup.PopupEntity(Loc.GetString("adt-empath-felt"), target, target, PopupType.SmallCaution);
    }

    private (string Condition, string Thought) ReadCondition(EntityUid target)
    {
        var thought = _random.Pick(Thoughts);
        var fraction = 1f;

        if (TryComp<DamageableComponent>(target, out var damageable) &&
            _thresholds.TryGetThresholdForState(target, MobState.Dead, out var dead) &&
            dead > 0)
        {
            fraction = 1f - (float)(damageable.TotalDamage / dead.Value);
        }

        if (TryComp<FlammableComponent>(target, out var flammable) && flammable.OnFire)
        {
            fraction -= 0.5f;
            thought = "adt-empath-thought-burning";
        }

        var key = fraction switch
        {
            > 0.8f => "adt-empath-condition-fine",
            > 0.6f => "adt-empath-condition-light",
            > 0.4f => "adt-empath-condition-moderate",
            > 0.2f => "adt-empath-condition-strong",
            _ => "adt-empath-condition-agony",
        };

        if (key == "adt-empath-condition-agony")
            thought = "adt-empath-thought-death";

        return (Loc.GetString("adt-empath-condition", ("target", target), ("state", Loc.GetString(key))), thought);
    }

    private string ReadMood(EntityUid target)
    {
        var key = _combat.IsInCombatMode(target)
            ? "adt-empath-mood-violent"
            : "adt-empath-mood-calm";

        return Loc.GetString("adt-empath-mood", ("target", target), ("mood", Loc.GetString(key)));
    }

    private string? ReadTarget(EntityUid target)
    {
        if (!TryComp<ADTEmpathMemoryComponent>(target, out var memory))
            return null;

        if (memory.LastTarget is not { } last || TerminatingOrDeleted(last))
            return null;

        return Loc.GetString("adt-empath-aim", ("target", target), ("victim", last));
    }

    private string? ReadObjectives(EntityUid target)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind) || mind.Objectives.Count == 0)
            return null;

        var titles = new List<string>();

        foreach (var objective in mind.Objectives)
        {
            if (_objectives.GetInfo(objective, mindId, mind) is { } info)
                titles.Add(info.Title);
        }

        if (titles.Count == 0)
            return null;

        return Loc.GetString("adt-empath-plans", ("target", target), ("plans", string.Join(", ", titles)));
    }

    private string? ReadNumbers(EntityUid target)
    {
        foreach (var item in _inventory.GetHandOrInventoryEntities(target))
        {
            if (!TryComp<BankCardComponent>(item, out var card) || card.AccountId is not { } account)
                continue;

            if (!_bank.TryGetAccount(account, out var bank))
                continue;

            return Loc.GetString("adt-empath-numbers",
                ("target", target),
                ("account", bank.AccountId),
                ("pin", bank.AccountPin));
        }

        return null;
    }

    private void Send(ActorComponent actor, string message)
    {
        var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

        _chat.ChatMessageToOne(ChatChannel.Server, message, wrapped, EntityUid.Invalid, false, actor.PlayerSession.Channel);
    }
}
