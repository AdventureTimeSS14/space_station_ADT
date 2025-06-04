using Content.Shared.ADT.HoloCigar.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Smoking;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.HoloCigar;

public sealed class HoloCigarSystem : EntitySystem
{
    private const string LitPrefix = "lit";
    private const string UnlitPrefix = "unlit";

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedItemSystem _items = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HoloCigarComponent, GetVerbsEvent<AlternativeVerb>>(OnAddInteractVerb);
        SubscribeLocalEvent<HoloCigarComponent, ComponentHandleState>(OnComponentHandleState);
    }

    private void OnAddInteractVerb(Entity<HoloCigarComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null)
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                HandleToggle(ent);
                _audio.PlayPvs(ent.Comp.SwitchAudio, ent, AudioParams.Default.WithVolume(3f));
                ent.Comp.Lit = !ent.Comp.Lit;
                Dirty(ent);
            },
            Message = Loc.GetString("holo-cigar-verb-desc"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/clock.svg.192dpi.png")),
            Text = Loc.GetString("holo-cigar-verb-text"),
        };

        args.Verbs.Add(verb);
    }

    private void HandleToggle(Entity<HoloCigarComponent> ent,
        AppearanceComponent? appearance = null,
        ClothingComponent? clothing = null)
    {
        if (!Resolve(ent, ref appearance, ref clothing) ||
            !_gameTiming.IsFirstTimePredicted)
            return;

        var state = ent.Comp.Lit ? SmokableState.Unlit : SmokableState.Lit;
        var prefix = ent.Comp.Lit ? UnlitPrefix : LitPrefix;

        _appearance.SetData(ent, SmokingVisuals.Smoking, state, appearance);
        _clothing.SetEquippedPrefix(ent, prefix, clothing);
        _items.SetHeldPrefix(ent, prefix);
    }

    private void OnComponentHandleState(Entity<HoloCigarComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not HoloCigarComponentState state)
            return;

        if (ent.Comp.Lit == state.Lit)
            return;

        ent.Comp.Lit = state.Lit;
        HandleToggle(ent);
    }
}
