using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.ADT.PaperOrigami;
using Content.Shared.ADT.PaperOrigami.Components;
using Content.Shared.Throwing;
using Robust.Shared.Utility;

namespace Content.Server.ADT.PaperOrigami;

public sealed class PaperOrigamiSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PaperOrigamiComponent, GetVerbsEvent<Verb>>(OnAddVerbIsPaperOrigami);
    }

    private void UpdateAppearance(EntityUid uid, PaperOrigamiComponent component, bool canMakeState, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance))
            return;

        component.CanMakeState = canMakeState;
        _appearance.SetData(uid, PaperOrigamiState.State, component.CanMakeState, appearance);

        if (canMakeState)
        {
            var throwing = EnsureComp<ThrowingAngleComponent>(uid);
            throwing.Angle = Angle.FromDegrees(90);
        }
        else
        {
            RemComp<ThrowingAngleComponent>(uid);
        }
    }

    private void OnAddVerbIsPaperOrigami(EntityUid uid, PaperOrigamiComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (TryComp<TagComponent>(uid, out var tagComponent))
        {
            var tags = tagComponent.Tags;
            if (tags.Contains("ADTDoNotMakeOrigami"))
            {
                return;
            }
        }

        Verb makePaperOrigamiAirplane = new()
        {
            Text = Loc.GetString("verb-paper-origami-airplane"),
            Category = VerbCategory.PaperOrigami,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Misc/bureaucracy.rsi"), "paperairplane"),

            Act = () =>
            {
                UpdateAppearance(uid, component, true);
            }
        };

        Verb makePaperOrigamiDefault = new()
        {
            Text = Loc.GetString("verb-paper-origami-default"),
            Category = VerbCategory.PaperOrigami,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Misc/bureaucracy.rsi"), "paper"),

            Act = () =>
            {
                UpdateAppearance(uid, component, false);
            }
        };

        args.Verbs.Add(makePaperOrigamiDefault);
        args.Verbs.Add(makePaperOrigamiAirplane);
    }
}
