using Content.Server.Bible.Components;
using Content.Shared.ADT.Heretic.Common;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.Heretic.EntitySystems;

// ADT: очистка космической руны библией — серверная часть CosmicRunesSystem (BibleComponent в ADT серверный)
public sealed class CosmicRuneBibleSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCosmicRuneComponent, AfterInteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<HereticCosmicRuneComponent> ent, ref AfterInteractUsingEvent args)
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
