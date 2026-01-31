using Content.Shared.ADT.Paper.Components;
using Content.Shared.Examine;
using Content.Shared.Paper;

namespace Content.Shared.ADT.Paper.Systems;

public sealed class ChalkboardSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChalkboardComponent, ExaminedEvent>(OnChalkboardExamined);
    }

    private void OnChalkboardExamined(Entity<ChalkboardComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryComp<PaperComponent>(entity, out var paper))
            return;

        if (string.IsNullOrWhiteSpace(paper.Content))
            return;

        args.PushMarkup(paper.Content);
    }
}
