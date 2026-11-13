using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Crushers.Systems;

public sealed class CrusherSpeedSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTCrusherSpeedOnDetonateComponent, CrusherMarkDetonatedEvent>(OnDetonated);
        SubscribeLocalEvent<ADTCrusherSpeedOnDetonateComponent, TrophyHolderExamineEvent>(OnTrophyExamine);
    }

    private void OnDetonated(Entity<ADTCrusherSpeedOnDetonateComponent> ent, ref CrusherMarkDetonatedEvent args)
    {
        var now = _timing.CurTime;

        if (now - ent.Comp.LastStack > ent.Comp.Duration)
            ent.Comp.Stacks = 0;

        ent.Comp.Stacks = Math.Min(ent.Comp.Stacks + 1, ent.Comp.MaxStacks);
        ent.Comp.LastStack = now;

        if (!_status.TrySetStatusEffectDuration(args.User, ent.Comp.SpeedEffect, out var effect, ent.Comp.Duration))
            return;

        var modifier = 1f + ent.Comp.SpeedPerStack * ent.Comp.Stacks;
        _movementMod.TryUpdateMovementStatus(args.User, effect.Value, modifier);
    }

    private void OnTrophyExamine(Entity<ADTCrusherSpeedOnDetonateComponent> ent, ref TrophyHolderExamineEvent args)
    {
        args.PushMarkup(Loc.GetString("adt-crusher-speed-examine",
            ("speed", (ent.Comp.SpeedPerStack * 100f).ToString("0.#")),
            ("max", (ent.Comp.SpeedPerStack * ent.Comp.MaxStacks * 100f).ToString("0.#")),
            ("duration", ent.Comp.Duration.TotalSeconds.ToString("0.#"))));
    }
}
