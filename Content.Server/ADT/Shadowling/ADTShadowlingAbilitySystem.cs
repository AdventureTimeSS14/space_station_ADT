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
    [Dependency] private ADTShadowlingSystem _shadowling = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private SharedStealthSystem _stealth = default!;
    [Dependency] private SharedPoweredLightSystem _poweredLight = default!;
    [Dependency] private SlimPoweredLightSystem _slimLight = default!;
    [Dependency] private SharedHandheldLightSystem _handheldLight = default!;
    [Dependency] private UnpoweredFlashlightSystem _flashlight = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedLanguageSystem _language = default!;
    [Dependency] private ADTBorgShutdownSystem _borgShutdown = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedNightVisionSystem _nightVision = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SmokeSystem _smoke = default!;
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private ApcSystem _apc = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private QuickDialogSystem _dialog = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private ADTDeafnessSystem _deafness = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private Content.Shared.StatusEffectNew.StatusEffectsSystem _statusNew = default!;
    [Dependency] private SharedGameTicker _gameTicker = default!;

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
