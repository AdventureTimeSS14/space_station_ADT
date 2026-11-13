using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Speech;

namespace Content.Shared.ADT.Body.OrganHealth;

public sealed class OrganFunctionsEventsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganFunctionsComponent, CanSeeAttemptEvent>(OnCanSeeAttempt);
        SubscribeLocalEvent<OrganFunctionsComponent, GetBlurEvent>(OnGetBlur);
        SubscribeLocalEvent<OrganFunctionsComponent, SpeakAttemptEvent>(OnSpeakAttempt);
    }

    private void OnCanSeeAttempt(Entity<OrganFunctionsComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (ent.Comp.Blind)
            args.Cancel();
    }

    private void OnGetBlur(Entity<OrganFunctionsComponent> ent, ref GetBlurEvent args)
    {
        args.Blur += ent.Comp.Blur;
    }

    private void OnSpeakAttempt(Entity<OrganFunctionsComponent> ent, ref SpeakAttemptEvent args)
    {
        if (ent.Comp.Mute)
            args.Cancel();
    }
}
