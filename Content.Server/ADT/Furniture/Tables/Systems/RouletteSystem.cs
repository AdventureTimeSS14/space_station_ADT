using Content.Shared.ADT.Furniture.Tables.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using System.Threading;

namespace Content.Server.ADT.Furniture.Tables.Systems;

public sealed class RouletteSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RouletteComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<RouletteComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, RouletteComponent component, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (component.State == RouletteState.Result && component.Result > 0)
        {
            var color = component.Result % 2 == 0 ? "black" : "red";
            args.PushMarkup(Loc.GetString("roulette-examine-result",
                ("number", component.Result),
                ("color", color)));
        }
        else if (component.State == RouletteState.Rolling)
        {
            args.PushMarkup(Loc.GetString("roulette-examine-rolling"));
        }
    }

    private void OnActivate(EntityUid uid, RouletteComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (component.State == RouletteState.Rolling)
        {
            _popup.PopupEntity(Loc.GetString("roulette-already-rolling"), uid, args.User);
            return;
        }

        args.Handled = true;

        if (component.State == RouletteState.Result)
        {
            component.State = RouletteState.Idle;
            component.Result = 0;
            _appearance.SetData(uid, RouletteVisuals.State, RouletteState.Idle);
            Dirty(uid, component);
        }

        component.Result = _random.Next(1, 37);
        component.State = RouletteState.Rolling;

        _appearance.SetData(uid, RouletteVisuals.State, RouletteState.Rolling);
        _appearance.SetData(uid, RouletteVisuals.Result, component.Result);

        Dirty(uid, component);

        var audioParams = new AudioParams
        {
            Volume = -8f,
            MaxDistance = 7f,
            RolloffFactor = 2f,
            ReferenceDistance = 0.5f
        };
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/ADT/Furniture/roulette-wheel-throw-1.ogg"), uid, audioParams);

        component.CancellationTokenSource?.Cancel();
        component.CancellationTokenSource = new CancellationTokenSource();
        Robust.Shared.Timing.Timer.Spawn(TimeSpan.FromSeconds(9.0), () =>
        {
            if (TerminatingOrDeleted(uid))
                return;

            SetResult(uid, component);
        }, component.CancellationTokenSource.Token);
    }

    public void SetResult(EntityUid uid, RouletteComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.State = RouletteState.Result;
        _appearance.SetData(uid, RouletteVisuals.State, RouletteState.Result);
        Dirty(uid, component);
    }
}
