using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Stack;
using Content.Server.Store.Components;
using Content.Shared.Access.Components;
using Content.Shared.ADT.CCVar;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.ADT.Economy;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Store.Components;

namespace Content.Server.ADT.Economy;

public sealed class ATMSystem : SharedATMSystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private BankCardSystem _bankCardSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private StackSystem _stackSystem = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ATMComponent, EntInsertedIntoContainerMessage>(OnCardInserted);
        SubscribeLocalEvent<ATMComponent, EntRemovedFromContainerMessage>(OnCardRemoved);
        SubscribeLocalEvent<ATMComponent, ATMRequestWithdrawMessage>(OnWithdrawRequest);
        SubscribeLocalEvent<ATMComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ATMComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ATMComponent, GotEmaggedEvent>(OnEmag);
        SubscribeLocalEvent<ATMComponent, SkimmerInstallDoAfterEvent>(OnSkimmerInstall);
    }

    private void OnEmag(EntityUid uid, ATMComponent component, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }

    private void OnComponentStartup(EntityUid uid, ATMComponent component, ComponentStartup args)
    {
        UpdateUiState(uid, -1, false, Loc.GetString("atm-ui-insert-card"));
    }

    private void OnInteractUsing(EntityUid uid, ATMComponent component, InteractUsingEvent args)
    {
        if (TryComp<SkimmerComponent>(args.Used, out var skimmer))
        {
            TryStartSkimInstall((uid, component), (args.Used, skimmer), args.User);
            args.Handled = true;
            return;
        }

        if (!TryComp<CurrencyComponent>(args.Used, out var currency) || !currency.Price.Keys.Contains(component.CurrencyType))
        {
            return;
        }

        if (!component.CardSlot.Item.HasValue)
        {
            _popupSystem.PopupEntity(Loc.GetString("atm-trying-insert-cash-error"), args.Target, args.User, PopupType.Medium);
            _audioSystem.PlayPvs(component.SoundDeny, uid);
            return;
        }

        var stack = Comp<StackComponent>(args.Used);
        var bankCard = Comp<BankCardComponent>(component.CardSlot.Item.Value);
        var amount = stack.Count;

        if (_random.Prob(component.ErrorChance))
        {
            Del(args.Used);
            _stackSystem.SpawnAtPosition(amount, _prototypeManager.Index<StackPrototype>(component.CreditStackPrototype), Transform(uid).Coordinates);
            _audioSystem.PlayPvs(component.SoundWithdrawCurrency, uid);
            _popupSystem.PopupEntity(Loc.GetString("atm-error"), uid);
            return;
        }

        _bankCardSystem.TryDeposit(bankCard.AccountId!.Value, amount, TransactionReason.AtmDeposit, Name(args.User));
        TrySkim((uid, component), bankCard.AccountId.Value, args.User);
        Del(args.Used);

        _audioSystem.PlayPvs(component.SoundInsertCurrency, uid);
        UpdateUiState(uid, _bankCardSystem.GetBalance(bankCard.AccountId.Value), true, Loc.GetString("atm-ui-select-withdraw-amount"));
    }

    private void OnCardInserted(EntityUid uid, ATMComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<BankCardComponent>(args.Entity, out var bankCard) || !bankCard.AccountId.HasValue)
        {
            _container.EmptyContainer(args.Container);
            return;
        }

        UpdateUiState(uid, _bankCardSystem.GetBalance(bankCard.AccountId.Value), true, Loc.GetString("atm-ui-select-withdraw-amount"));
    }

    private void OnCardRemoved(EntityUid uid, ATMComponent component, EntRemovedFromContainerMessage args)
    {
        UpdateUiState(uid, -1, false, Loc.GetString("atm-ui-insert-card"));
    }

    private void OnWithdrawRequest(EntityUid uid, ATMComponent component, ATMRequestWithdrawMessage args)
    {
        if (!TryComp<BankCardComponent>(component.CardSlot.Item, out var bankCard) || !bankCard.AccountId.HasValue)
        {
            if (component.CardSlot.ContainerSlot != null)
                _container.EmptyContainer(component.CardSlot.ContainerSlot);
            return;
        }

        if (!_bankCardSystem.TryGetAccount(bankCard.AccountId.Value, out var account) ||
            account.AccountPin != args.Pin && !HasComp<EmaggedComponent>(uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("atm-wrong-pin"), uid);
            _audioSystem.PlayPvs(component.SoundDeny, uid);
            return;
        }

        if (!_bankCardSystem.TryBurn(account.AccountId, args.Amount, TransactionReason.AtmWithdraw))
        {
            _popupSystem.PopupEntity(Loc.GetString("atm-not-enough-cash"), uid);
            _audioSystem.PlayPvs(component.SoundDeny, uid);
            return;
        }

        TrySkim((uid, component), account.AccountId, args.Actor);

        _stackSystem.SpawnAtPosition(args.Amount, _prototypeManager.Index<StackPrototype>(component.CreditStackPrototype), Transform(uid).Coordinates);
        _audioSystem.PlayPvs(component.SoundWithdrawCurrency, uid);

        UpdateUiState(uid, account.Balance, true, Loc.GetString("atm-ui-select-withdraw-amount"));
    }

    private void UpdateUiState(EntityUid uid, int balance, bool hasCard, string infoMessage)
    {
        var state = new ATMBuiState
        {
            AccountBalance = balance,
            HasCard = hasCard,
            InfoMessage = infoMessage
        };


        _ui.SetUiState(uid, ATMUiKey.Key, state);
    }

    private void TryStartSkimInstall(Entity<ATMComponent> atm, Entity<SkimmerComponent> skimmer, EntityUid user)
    {
        if (HasComp<ATMSkimmerComponent>(atm))
        {
            _popupSystem.PopupEntity(Loc.GetString("atm-skim-already-installed"), atm, user, PopupType.Medium);
            return;
        }

        if (!_bankCardSystem.TryGetUserAccountId(user, out var linkedAccountId))
        {
            _popupSystem.PopupEntity(Loc.GetString("atm-skim-no-linked-card"), atm, user, PopupType.MediumCaution);
            return;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, skimmer.Comp.AttachDelay,
            new SkimmerInstallDoAfterEvent(), atm.Owner, target: atm.Owner, used: skimmer.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        });
    }

    private void OnSkimmerInstall(EntityUid uid, ATMComponent component, SkimmerInstallDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || args.Used == null)
            return;

        if (!TryComp<SkimmerComponent>(args.Used.Value, out var skimmer) ||
            HasComp<ATMSkimmerComponent>(uid) ||
            !_bankCardSystem.TryGetUserAccountId(args.User, out var linkedAccountId))
            return;

        var installed = EnsureComp<ATMSkimmerComponent>(uid);
        installed.LinkedAccountId = linkedAccountId;
        Del(args.Used.Value);

        _popupSystem.PopupEntity(Loc.GetString("atm-skim-installed"), uid, args.User, PopupType.Medium);
        _adminLogger.Add(LogType.Economy, LogImpact.High,
            $"[Economy] card skimmer installed on {ToPrettyString(uid)} by {ToPrettyString(args.User):user}, linked account {linkedAccountId}");
    }

    private void TrySkim(Entity<ATMComponent> atm, int victimAccountId, EntityUid user)
    {
        if (!TryComp<ATMSkimmerComponent>(atm, out var skimmer) ||
            skimmer.ScansLeft <= 0 ||
            skimmer.LinkedAccountId == null ||
            skimmer.LinkedAccountId == victimAccountId)
            return;

        var balance = _bankCardSystem.GetBalance(victimAccountId);
        var amount = Math.Min((int) (balance * skimmer.StealFraction), skimmer.StealCap);
        if (amount <= 0)
            return;

        if (!_bankCardSystem.TryTransfer(victimAccountId, skimmer.LinkedAccountId.Value, amount,
                TransactionReason.Fraud, Loc.GetString("bank-initiator-skimmer")))
            return;

        skimmer.ScansLeft--;
        if (skimmer.ScansLeft <= 0)
            RemComp<ATMSkimmerComponent>(atm);

        _popupSystem.PopupEntity(Loc.GetString("atm-skim-victim", ("amount", amount)), user, user, PopupType.MediumCaution);
        _adminLogger.Add(LogType.Economy, LogImpact.High,
            $"[Economy] skimmer on {ToPrettyString(atm)} skimmed {amount}$ from account {victimAccountId}");
    }
}
