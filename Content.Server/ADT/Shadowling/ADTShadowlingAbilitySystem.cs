using Content.Server.Administration;
using Content.Server.ADT.Deafness;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Light.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.RoundEnd;
using Content.Server.ADT.Silicons.Borgs;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.NightVision;
using Content.Shared.ADT.Shadowling;
using Content.Shared.Actions;
using Content.Shared.Administration.Systems;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Gibbing;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Light;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Standing;
using Content.Shared.Stealth;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Shadowling;

public sealed partial class ADTShadowlingAbilitySystem : EntitySystem
{
    [Dependency] private readonly ADTShadowlingSystem _shadowling = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly SharedPoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly SlimPoweredLightSystem _slimLight = default!;
    [Dependency] private readonly SharedHandheldLightSystem _handheldLight = default!;
    [Dependency] private readonly UnpoweredFlashlightSystem _flashlight = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedLanguageSystem _language = default!;
    [Dependency] private readonly ADTBorgShutdownSystem _borgShutdown = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedNightVisionSystem _nightVision = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ApcSystem _apc = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly QuickDialogSystem _dialog = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly ADTDeafnessSystem _deafness = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly Content.Shared.StatusEffectNew.StatusEffectsSystem _statusNew = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeHatch();
        InitializePowers();
        InitializeThrall();
        InitializeProgression();
        InitializeAscension();
        InitializeDethrall();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateGuise();
    }

    public bool IsHiveMember(EntityUid uid)
    {
        return HasComp<ADTShadowlingComponent>(uid)
            || HasComp<ADTShadowlingThrallComponent>(uid)
            || HasComp<ADTLesserShadowlingComponent>(uid)
            || HasComp<ADTAscendantShadowlingComponent>(uid);
    }

    private bool CanUsePower(Entity<ADTShadowlingComponent> ent)
    {
        if (ent.Comp.Hatched)
            return true;

        _popup.PopupEntity(Loc.GetString("shadowling-not-hatched"), ent, ent);
        return false;
    }
}
