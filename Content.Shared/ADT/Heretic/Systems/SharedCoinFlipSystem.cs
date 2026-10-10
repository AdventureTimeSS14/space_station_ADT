//

using Content.Shared.Examine;
using Content.Shared.Heretic.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Systems;

public abstract partial class SharedCoinFlipSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoinFlipComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<CoinFlipComponent, ThrownEvent>(OnThrow);
        SubscribeLocalEvent<CoinFlipComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<CoinFlipComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.CurrentSide is { } side)
            args.PushMarkup(Loc.GetString("coin-flip-examine-message", ("side", Loc.GetString(side.Name))));
    }

    private void OnThrow(Entity<CoinFlipComponent> ent, ref ThrownEvent args)
    {
        TryFlip(ent, args.User);
    }

    private void OnUse(Entity<CoinFlipComponent> ent, ref UseInHandEvent args)
    {
        if (!TryFlip(ent, args.User))
            return;

        args.Handled = true;
    }

    private bool TryFlip(Entity<CoinFlipComponent> ent, EntityUid? user)
    {
        var now = Timing.CurTime;
        if (now < ent.Comp.FlipEndTime + ent.Comp.FlipDelay || ent.Comp.IsFlipping)
            return false;

        ent.Comp.FlipEndTime = now + ent.Comp.FlipTime;
        ent.Comp.IsFlipping = true;
        ent.Comp.User = user;
        Dirty(ent);

        Appearance.SetData(ent, CoinFlipVisuals.SpriteState, ent.Comp.FlippingSpriteState);
        _audio.PlayPredicted(ent.Comp.FlipSound, ent, user);

        return true;
    }
}
