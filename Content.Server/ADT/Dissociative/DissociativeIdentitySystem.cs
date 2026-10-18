using Content.Server.ADT.Dissociative.Components;
using Content.Server.ADT.Hallucinations.Systems;
using Content.Server.Antag;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.ADT.Dissociative;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.TTS;
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Clothing;
using Content.Shared.Database;
using Content.Shared.Ghost.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Dissociative;

public sealed partial class DissociativeIdentitySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SchizophreniaSystem _schizophrenia = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private LoadoutSystem _loadout = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DissociativeIdentityComponent, ComponentInit>(OnHostInit);
        SubscribeLocalEvent<DissociativeIdentityComponent, MapInitEvent>(OnHostMapInit);
        SubscribeLocalEvent<DissociativeIdentityComponent, ComponentShutdown>(OnHostShutdown);
        SubscribeLocalEvent<DissociativeIdentityComponent, MoveEvent>(OnHostMove);

        SubscribeLocalEvent<DissociativeAlterComponent, ComponentShutdown>(OnAlterShutdown);
        SubscribeLocalEvent<DissociativeAlterComponent, MoveEvent>(OnAlterMove);
        SubscribeLocalEvent<DissociativeAlterComponent, PlayerAttachedEvent>(OnAlterPlayerAttached);
        SubscribeLocalEvent<DissociativeAlterComponent, DissociativeManifestActionEvent>(OnManifestAction);

        InitializeFront();
        InitializeSetup();
        InitializeAntag();
        InitializeSurrender();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DissociativeIdentityComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Fronting && comp.FrontEnd <= _timing.CurTime)
                EndFront((uid, comp));
        }
    }

    private void OnHostInit(Entity<DissociativeIdentityComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _container.EnsureContainer<ContainerSlot>(ent, DissociativeIdentityComponent.ContainerId);
    }

    private void OnHostMapInit(Entity<DissociativeIdentityComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextFront = _timing.CurTime + ent.Comp.FrontCooldown;
        ent.Comp.LastAntipsychotic = _timing.CurTime;

        SpawnAlter(ent);
    }

    private void OnHostShutdown(Entity<DissociativeIdentityComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Fronting && ent.Comp.HostMind is { } hostMind && !TerminatingOrDeleted(hostMind))
            _mind.TransferTo(hostMind, null);

        _actions.RemoveAction(ent.Owner, ent.Comp.ReleaseActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.SurrenderActionEntity);

        if (ent.Comp.Alter is { } alter)
            QueueDel(alter);
    }

    private void OnAlterShutdown(Entity<DissociativeAlterComponent> ent, ref ComponentShutdown args)
    {
        foreach (var item in ent.Comp.Clothing)
            QueueDel(item);

        if (TryComp<DissociativeIdentityComponent>(ent.Comp.Host, out var host) && host.Alter == ent.Owner)
            host.Alter = null;
    }

    private void SpawnAlter(Entity<DissociativeIdentityComponent> ent)
    {
        if (!TryComp<HumanoidProfileComponent>(ent, out var profile) ||
            !_proto.Resolve(profile.Species, out var species))
            return;

        if (!TrySpawnInContainer(species.DollPrototype,
                ent,
                DissociativeIdentityComponent.ContainerId,
                out var alterUid,
                overrides: ent.Comp.AlterComponents))
            return;

        var alter = alterUid.Value;
        var alterComp = EnsureComp<DissociativeAlterComponent>(alter);
        alterComp.Host = ent;
        ent.Comp.Alter = alter;

        RemComp<GhostOnMoveComponent>(alter);
        _visualBody.CopyAppearanceFrom(ent.Owner, alter);

        if (TryComp<HumanoidProfileComponent>(alter, out var alterProfile))
        {
            _humanoidProfile.SetSex((alter, alterProfile), profile.Sex);
            _humanoidProfile.SetGender((alter, alterProfile), profile.Gender);
            _humanoidProfile.SetAge((alter, alterProfile), profile.Age);
        }

        if (TryComp<TTSComponent>(ent, out var hostTts))
            EnsureComp<TTSComponent>(alter).VoicePrototypeId = hostTts.VoicePrototypeId;

        if (TryComp<LanguageSpeakerComponent>(ent, out var hostLang))
        {
            var lang = EnsureComp<LanguageSpeakerComponent>(alter);
            lang.Languages = new(hostLang.Languages);
            lang.CurrentLanguage = hostLang.CurrentLanguage;
            Dirty(alter, lang);
        }

        var hostName = Name(ent);
        _meta.SetEntityName(alter, hostName);

        if (TryComp<GhostRoleComponent>(alter, out var ghostRole))
            ghostRole.RoleName = Loc.GetString("ghost-role-information-dissociative-alter-name", ("host", hostName));

        _schizophrenia.AddAsHallucination(ent, alter);

        _actions.AddAction(alter, ref alterComp.ManifestActionEntity, ent.Comp.ManifestAction);
        _actions.AddAction(alter, ref alterComp.FrontActionEntity, ent.Comp.FrontAction);
        _actions.AddAction(alter, ref alterComp.SetupActionEntity, ent.Comp.SetupAction);
        _actions.AddAction(ent, ref ent.Comp.SurrenderActionEntity, ent.Comp.SurrenderAction);
    }

    private void OnAlterPlayerAttached(Entity<DissociativeAlterComponent> ent, ref PlayerAttachedEvent args)
    {
        if (!TryComp<DissociativeIdentityComponent>(ent.Comp.Host, out var host) || host.Fronting)
            return;

        if (!ent.Comp.SetupDone)
            OpenSetup(ent, args.Player);

        if (!ent.Comp.AntagRolled)
            RollAntag(ent, args.Player);
    }

    #region Manifesting
    private void OnManifestAction(Entity<DissociativeAlterComponent> ent, ref DissociativeManifestActionEvent args)
    {
        if (args.Handled || !TryGetHost(ent, out var host))
            return;

        args.Handled = true;

        if (host.Value.Comp.Fronting)
        {
            _popup.PopupEntity(Loc.GetString("dissociative-not-in-control"), ent, ent);
            return;
        }

        if (ent.Comp.Manifested)
        {
            Retract(host.Value, ent);
            return;
        }

        if (_container.IsEntityInContainer(host.Value))
        {
            _popup.PopupEntity(Loc.GetString("dissociative-manifest-host-contained"), ent, ent);
            return;
        }

        RefreshClothing(host.Value, ent);
        _container.Remove(ent.Owner, host.Value.Comp.Container);
        ent.Comp.Manifested = true;
    }

    private void Retract(Entity<DissociativeIdentityComponent> host, Entity<DissociativeAlterComponent> alter)
    {
        if (!alter.Comp.Manifested)
            return;

        _container.Insert(alter.Owner, host.Comp.Container);
        alter.Comp.Manifested = false;
    }

    private void OnHostMove(Entity<DissociativeIdentityComponent> ent, ref MoveEvent args)
    {
        if (TryComp<DissociativeAlterComponent>(ent.Comp.Alter, out var alter))
            CheckLeash(ent, (ent.Comp.Alter.Value, alter));
    }

    private void OnAlterMove(Entity<DissociativeAlterComponent> ent, ref MoveEvent args)
    {
        if (TryComp<DissociativeIdentityComponent>(ent.Comp.Host, out var host))
            CheckLeash((ent.Comp.Host, host), ent);
    }

    private void CheckLeash(Entity<DissociativeIdentityComponent> host, Entity<DissociativeAlterComponent> alter)
    {
        if (!alter.Comp.Manifested || TerminatingOrDeleted(host) || TerminatingOrDeleted(alter))
            return;

        if (_container.IsEntityInContainer(host.Owner) ||
            !_transform.InRange(Transform(alter).Coordinates, Transform(host).Coordinates, host.Comp.LeashRange))
            Retract(host, alter);
    }

    private void RefreshClothing(Entity<DissociativeIdentityComponent> host, Entity<DissociativeAlterComponent> alter)
    {
        foreach (var item in alter.Comp.Clothing)
            Del(item);

        alter.Comp.Clothing.Clear();

        foreach (var slot in host.Comp.CopiedSlots)
        {
            if (!_inventory.TryGetSlotEntity(host, slot, out var worn) ||
                Prototype(worn.Value) is not { } proto)
                continue;

            if (!_inventory.SpawnItemInSlot(alter, slot, proto.ID, silent: true, force: true) ||
                !_inventory.TryGetSlotEntity(alter, slot, out var copy))
                continue;

            EnsureComp<UnremoveableComponent>(copy.Value);
            alter.Comp.Clothing.Add(copy.Value);
        }
    }
    #endregion

    private bool TryGetHost(Entity<DissociativeAlterComponent> alter, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Entity<DissociativeIdentityComponent>? host)
    {
        host = null;
        if (!TryComp<DissociativeIdentityComponent>(alter.Comp.Host, out var comp) || TerminatingOrDeleted(alter.Comp.Host))
            return false;

        host = (alter.Comp.Host, comp);
        return true;
    }
}
