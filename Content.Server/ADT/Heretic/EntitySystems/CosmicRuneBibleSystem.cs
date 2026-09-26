using Content.Shared.ADT.Wizard.FadingTimedDespawn;
using Content.Server.Bible.Components;
using Content.Shared.ADT.Heretic.Common;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.Heretic.EntitySystems;

// ADT: server-side bible-cleanse part of CosmicRunesSystem
public sealed class CosmicRuneBibleSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        // ADT: InteractUsingEvent, not AfterInteractUsingEvent (avoid dupe sub with shared system)
        SubscribeLocalEvent<HereticCosmicRuneComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<HereticCosmicRuneComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || HasComp<FadingTimedDespawnComponent>(ent))
            return;

        if (!TryComp(args.Used, out BibleComponent? bible) ||
            !HasComp<BibleUserComponent>(args.User) || !TryComp(args.Used, out UseDelayComponent? useDelay) ||
            _useDelay.IsDelayed((args.Used, useDelay)))
            return;

        _useDelay.TryResetDelay(args.Used, false, useDelay);
        _audio.PlayPvs(bible.HealSoundPath, Transform(ent).Coordinates);
        EnsureComp<FadingTimedDespawnComponent>(ent).Lifetime = 0f;
        args.Handled = true;
    }
}
