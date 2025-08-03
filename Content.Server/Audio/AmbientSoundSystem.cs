using Content.Server.ADT.Temperature; //ADT-Tweak-Bonfire
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Mobs;
using Content.Shared.Power;

namespace Content.Server.Audio;

public sealed class AmbientSoundSystem : SharedAmbientSoundSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AmbientOnPoweredComponent, PowerChangedEvent>(HandlePowerChange);
        SubscribeLocalEvent<AmbientOnPoweredComponent, PowerNetBatterySupplyEvent>(HandlePowerSupply);
        SubscribeLocalEvent<ADTFlammableAmbientSoundComponent, OnFireChangedEvent>(OnFireChanged); //ADT bonfire moment
    }

    private void HandlePowerSupply(EntityUid uid, AmbientOnPoweredComponent component, ref PowerNetBatterySupplyEvent args)
    {
        SetAmbience(uid, args.Supply);
    }

    private void HandlePowerChange(EntityUid uid, AmbientOnPoweredComponent component, ref PowerChangedEvent args)
    {
        SetAmbience(uid, args.Powered);
    }

    //ADT bonfire
    private void OnFireChanged(Entity<ADTFlammableAmbientSoundComponent> ent, ref OnFireChangedEvent args)
    {
        SetAmbience(ent, args.OnFire);
    }
    //ADT bonfire end
}
