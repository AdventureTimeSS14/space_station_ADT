using System.Numerics;
using Content.Client.Camera;
using Content.Shared.ADT.Heretic.Components.Moon;
using Content.Shared.Camera;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.ADT.Heretic.Systems.Moon;

public sealed class MoonConfusionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly CameraRecoilSystem _recoil = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private float _accum;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InputMoverComponent, MoveInputEvent>(OnMoveInput);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var player = _player.LocalEntity;
        if (player == null || !TryComp<MoonConfusedComponent>(player, out var confused))
            return;

        _accum += frameTime;
        if (_accum < confused.ShakeInterval)
            return;

        _accum = 0f;
        var strength = confused.ShakeStrength;
        var kick = _random.NextVector2(-1f, 1f) * strength;
        _recoil.KickCamera(player.Value, kick);
    }

    private void OnMoveInput(Entity<InputMoverComponent> ent, ref MoveInputEvent args)
    {
        if (ent.Owner != _player.LocalEntity)
            return;
        if (!HasComp<MoonConfusedComponent>(ent))
            return;

        var buttons = ent.Comp.HeldMoveButtons;

        var up = buttons.HasFlag(MoveButtons.Up);
        var down = buttons.HasFlag(MoveButtons.Down);
        var left = buttons.HasFlag(MoveButtons.Left);
        var right = buttons.HasFlag(MoveButtons.Right);

        buttons &= ~(MoveButtons.Up | MoveButtons.Down | MoveButtons.Left | MoveButtons.Right);

        if (up)
            buttons |= MoveButtons.Down;
        if (down)
            buttons |= MoveButtons.Up;
        if (left)
            buttons |= MoveButtons.Right;
        if (right)
            buttons |= MoveButtons.Left;

        ent.Comp.HeldMoveButtons = buttons;
        Dirty(ent, ent.Comp);
    }
}