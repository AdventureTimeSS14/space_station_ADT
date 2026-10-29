using Content.Shared.ADT.Drawing;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Drawing.View;

public sealed class PaintingVerbSystem : EntitySystem
{
    private static readonly SpriteSpecifier VerbIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"));

    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintingComponent, GetVerbsEvent<ExamineVerb>>(OnPaintingVerbs);
        SubscribeLocalEvent<PaintingFrameComponent, GetVerbsEvent<ExamineVerb>>(OnFrameVerbs);
        SubscribeLocalEvent<PaintingComponent, ActivateInWorldEvent>(OnPaintingActivate);
        SubscribeLocalEvent<PaintingComponent, UseInHandEvent>(OnPaintingUse);
        SubscribeLocalEvent<PaintingFrameComponent, ActivateInWorldEvent>(OnFrameActivate);
    }

    private void OnPaintingActivate(Entity<PaintingComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!args.Handled && ent.Comp.PaintingUid is { } painting && !ent.Comp.Hidden && TryOpen(args.User, painting))
            args.Handled = true;
    }

    private void OnPaintingUse(Entity<PaintingComponent> ent, ref UseInHandEvent args)
    {
        if (!args.Handled && ent.Comp.PaintingUid is { } painting && !ent.Comp.Hidden && TryOpen(args.User, painting))
            args.Handled = true;
    }

    private void OnFrameActivate(Entity<PaintingFrameComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !TryGetFramePainting(ent.Owner, out var painting))
            return;

        if (TryOpen(args.User, painting))
            args.Handled = true;
    }

    private bool TryOpen(EntityUid user, Guid painting)
    {
        if (!_timing.IsFirstTimePredicted || user != _player.LocalEntity)
            return false;

        new PaintingViewerWindow(painting).OpenCentered();
        return true;
    }

    private bool TryGetFramePainting(EntityUid frame, out Guid painting)
    {
        painting = default;
        if (!_appearance.TryGetData<string>(frame, PaintingFrameVisuals.Painting, out var key) ||
            !Guid.TryParse(key, out painting))
        {
            return false;
        }

        return !_appearance.TryGetData<bool>(frame, PaintingFrameVisuals.Hidden, out var hidden) || !hidden;
    }

    private void OnPaintingVerbs(Entity<PaintingComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (ent.Comp.PaintingUid is { } painting && !ent.Comp.Hidden)
            AddVerb(args, painting);
    }

    private void OnFrameVerbs(Entity<PaintingFrameComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (TryGetFramePainting(ent.Owner, out var painting))
            AddVerb(args, painting);
    }

    private void AddVerb(GetVerbsEvent<ExamineVerb> args, Guid painting)
    {
        args.Verbs.Add(new ExamineVerb
        {
            Text = Loc.GetString("adt-painting-verb-view"),
            Icon = VerbIcon,
            Act = () => new PaintingViewerWindow(painting).OpenCentered(),
            ClientExclusive = true,
            CloseMenu = true,
        });
    }
}
