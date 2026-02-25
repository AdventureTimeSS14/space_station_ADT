using Content.Server.ADT.Economy;
using Content.Server.Chat.Systems;
using Content.Server.Medical;
using Content.Server.Power.EntitySystems;
using Content.Shared.ADT.Economy;
using Content.Shared.ADT.Predictor;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Medical;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.ADT.Predictor;

//ADT-Tweak: Predictor Machine
public sealed class PredictorMachineSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BankCardSystem _bankCard = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PredictorMachineComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PredictorMachineComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<PredictorMachineComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<EmaggedComponent, ComponentInit>(OnEmaggedComponentInit);
    }

    private void OnPowerChanged(EntityUid uid, PredictorMachineComponent component, ref PowerChangedEvent args)
    {
        UpdateAppearance(uid, component);
    }

    private void OnInteractUsing(EntityUid uid, PredictorMachineComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<EmagComponent>(args.Used))
            return;

        var wrenchQuality = "Anchoring";
        if (_toolSystem.HasQuality(args.Used, wrenchQuality))
            return;

        BankCardComponent? bankCard = null;

        if (TryComp<PdaComponent>(args.Used, out var pda) && pda.ContainedId is { Valid: true } id)
        {
            if (TryComp<BankCardComponent>(id, out var card) && card.AccountId.HasValue)
            {
                bankCard = card;
            }
        }
        else if (TryComp<BankCardComponent>(args.Used, out var directCard) && directCard.AccountId.HasValue)
        {
            bankCard = directCard;
        }

        if (bankCard == null || !bankCard.AccountId.HasValue)
        {
            _popup.PopupEntity(Loc.GetString("predictor-machine-no-id"), uid, args.User);
            _audio.PlayPvs(component.SoundDeny, uid);
            return;
        }

        if (!_powerReceiver.IsPowered(uid))
        {
            _popup.PopupEntity(Loc.GetString("predictor-machine-no-power"), uid, args.User);
            return;
        }

        if (component.IsAnimating)
            return;

        if (!_bankCard.TryGetAccount(bankCard.AccountId.Value, out var account) || account.Balance < component.Price)
        {
            _popup.PopupEntity(Loc.GetString("predictor-machine-insufficient-funds"), uid, args.User);
            _audio.PlayPvs(component.SoundDeny, uid);
            return;
        }

        if (!_bankCard.TryChangeBalance(bankCard.AccountId.Value, -component.Price))
        {
            _popup.PopupEntity(Loc.GetString("predictor-machine-payment-failed"), uid, args.User);
            _audio.PlayPvs(component.SoundDeny, uid);
            return;
        }

        _audio.PlayPvs(component.SoundPayment, uid);

        MakePrediction(uid, component, args.User);
        args.Handled = true;
    }

    private void MakePrediction(EntityUid uid, PredictorMachineComponent component, EntityUid user)
    {
        component.IsAnimating = true;
        Dirty(uid, component);

        _popup.PopupEntity(Loc.GetString("predictor-machine-payment-success", ("amount", component.Price)), uid, user);

        var isEmagged = HasComp<EmaggedComponent>(uid);
        var packId = isEmagged ? component.EmaggedPredictionsPack : component.NormalPredictionsPack;

        if (string.IsNullOrEmpty(packId) || !_prototypeManager.TryIndex<PredictorPackPrototype>(packId, out var pack))
        {
            component.IsAnimating = false;
            Dirty(uid, component);
            UpdateAppearance(uid, component);
            return;
        }

        if (pack.Predictions.Count == 0)
        {
            component.IsAnimating = false;
            Dirty(uid, component);
            UpdateAppearance(uid, component);
            return;
        }

        string prediction;
        bool isSpecial = false;

        if (!isEmagged && _random.Prob(component.SpecialPredictionChance))
        {
            var specialRoll = _random.Next(2);
            if (specialRoll == 0)
            {
                prediction = Loc.GetString("predictor-special-omnizine");
                isSpecial = true;
            }
            else
            {
                prediction = Loc.GetString("predictor-special-dylovene");
                isSpecial = true;
            }
        }
        else
        {
            prediction = Loc.GetString(_random.Pick(pack.Predictions));
        }

        _chat.TrySendInGameICMessage(uid, prediction, InGameICChatType.Speak, false);

        if (isSpecial)
        {
            ApplySpecialEffect(user, prediction);
        }

        var isEmaggedForState = HasComp<EmaggedComponent>(uid);
        _appearance.SetData(uid, PredictorMachineVisuals.State, isEmaggedForState ? PredictorMachineState.EmaggedPredicting : PredictorMachineState.Predicting);

        var animationTime = 2.0f * component.AnimationCycles;

        Timer.Spawn(TimeSpan.FromSeconds(animationTime), () =>
        {
            if (Deleted(uid) || !TryComp<PredictorMachineComponent>(uid, out var comp))
                return;

            comp.IsAnimating = false;
            Dirty(uid, comp);
            UpdateAppearance(uid, comp);
        });
    }

    private void ApplySpecialEffect(EntityUid user, string prediction)
    {
        if (!TryComp<BloodstreamComponent>(user, out var bloodstream))
            return;

        var omnizinePrediction = Loc.GetString("predictor-special-omnizine");
        var dylovenePrediction = Loc.GetString("predictor-special-dylovene");

        if (prediction == omnizinePrediction)
        {
            var solution = new Solution();
            solution.AddReagent(new ReagentId("Omnizine", null), FixedPoint2.New(10));
            _bloodstream.TryAddToChemicals((user, bloodstream), solution);
        }
        else if (prediction == dylovenePrediction)
        {
            if (_random.Prob(0.5f))
            {
                var solution = new Solution();
                solution.AddReagent(new ReagentId("Dylovene", null), FixedPoint2.New(5));
                _bloodstream.TryAddToChemicals((user, bloodstream), solution);
            }
            else
            {
                _vomit.Vomit(user);
            }
        }
    }

    private void OnEmagged(EntityUid uid, PredictorMachineComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    private void OnEmaggedComponentInit(EntityUid uid, EmaggedComponent component, ComponentInit args)
    {
        if (TryComp<PredictorMachineComponent>(uid, out var predictorComponent))
        {
            UpdateAppearance(uid, predictorComponent);
        }
    }

    private void UpdateAppearance(EntityUid uid, PredictorMachineComponent component)
    {
        var isEmagged = HasComp<EmaggedComponent>(uid);

        if (!_powerReceiver.IsPowered(uid))
        {
            _appearance.SetData(uid, PredictorMachineVisuals.State, PredictorMachineState.Off);
            return;
        }

        if (component.IsAnimating)
        {
            _appearance.SetData(uid, PredictorMachineVisuals.State, isEmagged ? PredictorMachineState.EmaggedPredicting : PredictorMachineState.Predicting);
            return;
        }

        _appearance.SetData(uid, PredictorMachineVisuals.State, isEmagged ? PredictorMachineState.EmaggedOn : PredictorMachineState.On);
    }
}
