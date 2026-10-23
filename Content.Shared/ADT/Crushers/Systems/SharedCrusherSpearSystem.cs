using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Examine;
using Content.Shared.Item;

namespace Content.Shared.ADT.Crushers.Systems;

public abstract class SharedCrusherSpearSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTCrusherSpearThrowComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<ADTCrusherSpearThrowComponent, ExaminedEvent>(OnExamined);
    }

    private void OnPickupAttempt(Entity<ADTCrusherSpearThrowComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (ent.Comp.Thrower == null)
            return;

        args.Cancel();
    }

    private void OnExamined(Entity<ADTCrusherSpearThrowComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("adt-crusher-spear-examine",
            ("range", ent.Comp.Range.ToString("0.#")),
            ("cooldown", ent.Comp.Cooldown.TotalSeconds.ToString("0.#"))));
    }
}
