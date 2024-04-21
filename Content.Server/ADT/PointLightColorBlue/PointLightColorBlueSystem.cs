using Content.Server.Administration.Commands;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.Mobs;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.ADT.PointLightColorBlue;
using Content.Shared.Interaction.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Timers;
using System.ComponentModel;
using System.Linq;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

public sealed class PointLightColorBlueSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly PointLightSystem _light = default!;

    //[Dependency] private readonly PointLightComponent _lightCom = = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightColorBlueComponent, ComponentStartup>(OnComponentStartup);
        //SubscribeLocalEvent<PointLightColorBlueComponent, MobStateChangedEvent>(OnMobState);
    }
    // /// <summary>
    // /// On death removes active comps.
    // /// </summary>
    // private void OnMobState(EntityUid uid, PointLightColorBlueComponent component, MobStateChangedEvent args)
    // {
    //     if (args.NewMobState == MobState.Dead || component == null)
    //     {
    //         RemComp<PointLightColorBlueComponent>(uid);
    //         //var damageSpec = new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>("Genetic"), 300);
    //         //_damageableSystem.TryChangeDamage(uid, damageSpec);
    //     }
    // }
    private void OnComponentStartup(EntityUid uid, PointLightColorBlueComponent component, ComponentStartup args)
    {
        AddComp<PointLightComponent>(uid);
        _light.SetEnabled(uid, true);
        _light.SetColor(uid, Color.FromHex("#005f9e"));
        _light.SetRadius(uid, 1.2f);
        _light.SetEnergy(uid, 160f);
        _audioSystem.PlayPvs(component.Sound, uid); //, AudioParams.Default.WithVolume(component.Sound));
        //_light.
    }
}
