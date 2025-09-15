using Content.Shared.ADT.Roulette.Components;
using Content.Shared.Verbs;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Roulette.Systems;

public sealed class RouletteVerbSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RouletteComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<RouletteComponent, ComponentStartup>(OnRouletteStartup);
        SubscribeLocalEvent<RouletteComponent, GotEquippedHandEvent>(OnGotEquippedHand);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RouletteComponent>();
        while (query.MoveNext(out var uid, out var roulette))
        {
            UpdateRoulette(uid, roulette, frameTime);
            UpdateCooldown(uid, roulette);
        }
    }

    private void OnRouletteStartup(EntityUid uid, RouletteComponent component, ComponentStartup args)
    {
        component.CanSpin = true;
        component.IsSpinning = false;
        component.CurrentSpinSpeed = 0f;
        component.CurrentRotation = 0f;

        if (component.Friction <= 0f || component.Friction >= 1f)
            component.Friction = 0.998f;
    }

    private void OnGotEquippedHand(EntityUid uid, RouletteComponent component, GotEquippedHandEvent args)
    {
        component.IsSpinning = false;
        component.CurrentSpinSpeed = 0f;
        component.CurrentRotation = 0f;
        component.CanSpin = true;
    }

    private void OnGetInteractionVerbs(EntityUid uid, RouletteComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!component.CanSpin || component.IsSpinning)
            return;

        if (_hands.IsHolding(args.User, uid))
            return;

        var verb = new InteractionVerb
        {
            Act = () => SpinRoulette(uid, component),
            Text = Loc.GetString("verb-roulette-spin-text"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png")),
            Priority = 1
        };

        args.Verbs.Add(verb);
    }

    private void SpinRoulette(EntityUid uid, RouletteComponent roulette)
    {
        if (roulette.IsSpinning)
            return;

        var currentTime = _gameTiming.CurTime;
        if (currentTime - roulette.LastSpinTime < TimeSpan.FromSeconds(roulette.SpinCooldown))
            return;

        var spinForce = _random.NextFloat(roulette.MinSpinForce, roulette.MaxSpinForce);
        if (_random.NextFloat() > 0.5f)
            spinForce = -spinForce;

        roulette.CurrentSpinSpeed = spinForce;
        roulette.IsSpinning = true;
        roulette.CanSpin = false;
        roulette.LastSpinTime = currentTime;
    }

    private void UpdateRoulette(EntityUid uid, RouletteComponent roulette, float frameTime)
    {
        if (!roulette.IsSpinning)
            return;

        roulette.CurrentRotation += roulette.CurrentSpinSpeed * frameTime;
        roulette.CurrentSpinSpeed *= roulette.Friction;

        if (Math.Abs(roulette.CurrentSpinSpeed) < 1f)
        {
            roulette.CurrentSpinSpeed = 0f;
            roulette.IsSpinning = false;
        }
    }

    private void UpdateCooldown(EntityUid uid, RouletteComponent roulette)
    {
        if (roulette.CanSpin || roulette.IsSpinning)
            return;

        var currentTime = _gameTiming.CurTime;
        if (currentTime - roulette.LastSpinTime >= TimeSpan.FromSeconds(roulette.SpinCooldown))
        {
            roulette.CanSpin = true;
        }
    }
}
