using Content.Server.Administration.Systems;
using Content.Server.Antag;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid;
using Content.Server.Mind.Commands;
using Content.Server.Roles;
using Content.Server.Temperature.Components;
using Content.Shared.Temperature.Components;
using Content.Shared.Administration.Systems;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Heretic;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Popups;
using Robust.Shared.Log;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class GhoulSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private readonly Dictionary<EntityUid, GhoulStoredComponents> _storedComponents = new();

    public void GhoulifyEntity(Entity<GhoulComponent> ent)
    {
        // Защита от повторного вызова
        if (_storedComponents.ContainsKey(ent))
        {
            Log.Warning($"[GhoulSystem] GhoulifyEntity вызван повторно для {ToPrettyString(ent)}! Пропускаем.");
            return;
        }

        // Сохраняем компоненты
        var stored = SaveComponents(ent);
        _storedComponents[ent] = stored;

        // Удаляем компоненты
        RemComp<RespiratorComponent>(ent);
        RemComp<BarotraumaComponent>(ent);
        RemComp<HungerComponent>(ent);
        RemComp<ThirstComponent>(ent);
        RemComp<ReproductiveComponent>(ent);
        RemComp<ReproductivePartnerComponent>(ent);
        RemComp<TemperatureComponent>(ent);

        // Сохраняем оригинальные фракции перед заменой
        if (TryComp<NpcFactionMemberComponent>(ent, out var factionComp))
        {
            ent.Comp.OriginalFactions = factionComp.Factions.Select(f => f.Id).ToList();
        }

        var hasMind = _mind.TryGetMind(ent, out var mindId, out var mind);
        if (hasMind && ent.Comp.BoundHeretic != null)
            SendBriefing(ent, mindId, mind);

        if (TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
        {
            // make them "have no eyes" and grey
            // this is clearly a reference to grey tide
            var greycolor = Color.FromHex("#505050");
            _humanoid.SetSkinColor(ent, greycolor, true, false, humanoid);
            _humanoid.SetBaseLayerColor(ent, HumanoidVisualLayers.Eyes, greycolor, true, humanoid);
        }

        _rejuvenate.PerformRejuvenate(ent);
        if (TryComp<MobThresholdsComponent>(ent, out var th))
        {
            _threshold.SetMobStateThreshold(ent, ent.Comp.TotalHealth, MobState.Dead, th);
            _threshold.SetMobStateThreshold(ent, ent.Comp.TotalHealth / 1.25f, MobState.Critical, th);
        }

        MakeSentientCommand.MakeSentient(ent, EntityManager, _mind);

        if (!HasComp<GhostRoleComponent>(ent) && !hasMind)
        {
            var ghostRole = EnsureComp<GhostRoleComponent>(ent);
            ghostRole.RoleName = Loc.GetString("ghostrole-ghoul-name");
            ghostRole.RoleDescription = Loc.GetString("ghostrole-ghoul-desc");
            ghostRole.RoleRules = Loc.GetString("ghostrole-ghoul-rules");
        }

        if (!HasComp<GhostRoleMobSpawnerComponent>(ent) && !hasMind)
            EnsureComp<GhostTakeoverAvailableComponent>(ent);

        _faction.ClearFactions((ent, null));
        _faction.AddFaction((ent, null), "Heretic");
    }

    private void SendBriefing(Entity<GhoulComponent> ent, EntityUid mindId, MindComponent? mind)
    {
        var brief = Loc.GetString("heretic-ghoul-greeting-noname");
        EntityUid? master = EntityManager.GetEntity(ent.Comp.BoundHeretic);

        if (master.HasValue)
            brief = Loc.GetString("heretic-ghoul-greeting", ("ent", Identity.Entity(master.Value, EntityManager)));

        var sound = new SoundPathSpecifier("/Audio/ADT/Heretic/Ambience/Antag/Heretic/heretic_gain.ogg");
        _antag.SendBriefing(ent, brief, Color.MediumPurple, sound);

        if (!TryComp<GhoulRoleComponent>(ent, out _))
            AddComp<GhoulRoleComponent>(mindId, new(), overwrite: true);

        if (!TryComp<RoleBriefingComponent>(ent, out var rolebrief))
            AddComp(mindId, new RoleBriefingComponent() { Briefing = brief }, overwrite: true);
        else rolebrief.Briefing += $"\n{brief}";
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhoulComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GhoulComponent, AttackAttemptEvent>(OnTryAttack);
        SubscribeLocalEvent<GhoulComponent, TakeGhostRoleEvent>(OnTakeGhostRole);
        SubscribeLocalEvent<GhoulComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<GhoulComponent, MobStateChangedEvent>(OnMobStateChange);
    }

    private void OnInit(Entity<GhoulComponent> ent, ref ComponentInit args)
    {
        foreach (var look in _lookup.GetEntitiesInRange<HereticComponent>(Transform(ent).Coordinates, 1.5f))
        {
            if (ent.Comp.BoundHeretic == null)
                ent.Comp.BoundHeretic = GetNetEntity(look);
            else break;
        }

        GhoulifyEntity(ent);
    }
    private void OnTakeGhostRole(Entity<GhoulComponent> ent, ref TakeGhostRoleEvent args)
    {
        var hasMind = _mind.TryGetMind(ent, out var mindId, out var mind);
        if (hasMind)
            SendBriefing(ent, mindId, mind);
    }

    private void OnTryAttack(Entity<GhoulComponent> ent, ref AttackAttemptEvent args)
    {
        // prevent attacking owner and other heretics
        if (args.Target == ent.Owner
        || HasComp<HereticComponent>(args.Target))
            args.Cancel();
    }

    private void OnExamine(Entity<GhoulComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("examine-system-cant-see-entity"));
    }

    private void OnMobStateChange(Entity<GhoulComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RevertFromGhoul(ent);
    }

    public void RevertFromGhoul(Entity<GhoulComponent> ent)
    {
        var originalFactions = new List<string>(ent.Comp.OriginalFactions);

        // Восстанавливаем сохранённые компоненты
        if (_storedComponents.TryGetValue(ent, out var stored))
        {
            RestoreComponents(ent, stored);
            _storedComponents.Remove(ent);
        }

        RemComp<GhoulComponent>(ent);

        if (_mind.TryGetMind(ent, out var mindId, out _))
        {
            RemComp<GhoulRoleComponent>(mindId);
        }

        _faction.ClearFactions((ent, null));
        foreach (var faction in originalFactions)
        {
            _faction.AddFaction((ent, null), faction);
        }

        _popup.PopupEntity(Loc.GetString("ghoul-death-revert"), ent, PopupType.LargeCaution);
    }
}
