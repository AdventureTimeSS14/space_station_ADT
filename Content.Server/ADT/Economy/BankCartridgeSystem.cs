using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.ADT.Economy;
using Content.Shared.PDA;

namespace Content.Server.ADT.Economy;

public sealed class BankCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly BankCardSystem _bankCardSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BankCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<BankCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<BankCartridgeComponent, CartridgeAddedEvent>(OnInstall);
        SubscribeLocalEvent<BankCartridgeComponent, CartridgeRemovedEvent>(OnRemove);
    }

    private void OnRemove(EntityUid uid, BankCartridgeComponent component, CartridgeRemovedEvent args)
    {
        component.Loader = null;
    }

    private void OnInstall(EntityUid uid, BankCartridgeComponent component, CartridgeAddedEvent args)
    {
        component.Loader = args.Loader;
    }

    private void OnAccountLink(EntityUid uid, BankCartridgeComponent component, BankAccountLinkMessage args)
    {
        if (!_bankCardSystem.TryGetAccount(args.AccountId, out var account) || args.Pin != account.AccountPin ||
            account.CommandBudgetAccount)
        {
            component.AccountLinkResult = Loc.GetString("bank-program-ui-link-error");
            return;
        }

        component.AccountLinkResult = Loc.GetString("bank-program-ui-link-success");

        if (args.AccountId != component.AccountId)
        {
            if (component.AccountId != null &&
                _bankCardSystem.TryGetAccount(component.AccountId.Value, out var oldAccount) &&
                oldAccount.CartridgeUid == uid)
                oldAccount.CartridgeUid = null;

            if (account.CartridgeUid != null)
                Comp<BankCartridgeComponent>(account.CartridgeUid.Value).AccountId = null;

            account.CartridgeUid = uid;
            component.AccountId = args.AccountId;
        }

        if (!TryComp(GetEntity(args.LoaderUid), out PdaComponent? pda) || !pda.ContainedId.HasValue ||
            HasComp<BankCardComponent>(pda.ContainedId.Value))
            return;

        var bankCard = AddComp<BankCardComponent>(pda.ContainedId.Value);
        bankCard.AccountId = account.AccountId;
    }

    private void OnUiReady(EntityUid uid, BankCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    private void OnUiMessage(EntityUid uid, BankCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is BankAccountLinkMessage message)
            OnAccountLink(uid, component, message);

        UpdateUiState(uid, GetEntity(args.LoaderUid), component);
    }

    private void UpdateUiState(EntityUid cartridgeUid, EntityUid loaderUid, BankCartridgeComponent? component = null)
    {
        if (!Resolve(cartridgeUid, ref component))
            return;

        var accountLinkMessage = Loc.GetString("bank-program-ui-link-program") + '\n';
        if (TryComp(loaderUid, out PdaComponent? pda) && pda.ContainedId.HasValue)
        {
            accountLinkMessage += TryComp(pda.ContainedId.Value, out BankCardComponent? bankCard)
                ? Loc.GetString("bank-program-ui-link-id-card-linked", ("account", bankCard.AccountId!.Value))
                : Loc.GetString("bank-program-ui-link-id-card");
        }
        else
        {
            accountLinkMessage += Loc.GetString("bank-program-ui-link-no-id-card");
        }

        var state = new BankCartridgeUiState
        {
            AccountLinkResult = component.AccountLinkResult,
            AccountLinkMessage = accountLinkMessage
        };

        if (component.AccountId != null && _bankCardSystem.TryGetAccount(component.AccountId.Value, out var account))
        {
            state.Balance = account.Balance;
            state.AccountId = account.AccountId;
            state.OwnerName = account.Name;
        }
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }

    public void UpdateUiState(EntityUid cartridgeUid)
    {
        if (!TryComp(cartridgeUid, out BankCartridgeComponent? component) || component.Loader == null)
            return;

        UpdateUiState(cartridgeUid, component.Loader.Value, component);
    }
}
