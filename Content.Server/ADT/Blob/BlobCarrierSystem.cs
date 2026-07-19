// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Blob;
using Content.Shared.ADT.Blob.Components;
using Content.Shared.ADT.Blob.Events;
using Content.Server.Actions;
using Content.Shared.Gibbing;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Server.ADT.Language;
using Content.Shared.ADT.Language;
using Content.Shared.Popups;

namespace Content.Server.ADT.Blob;

public sealed class BlobCarrierSystem : SharedBlobCarrierSystem
{
    [Dependency] private readonly BlobCoreSystem _blobCoreSystem = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly GhostRoleSystem _ghost = default!;
    [Dependency] private readonly GibbingSystem _gibbingSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobCarrierComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<BlobCarrierComponent, TransformToBlobActionEvent>(OnTransformToBlobChanged);

        SubscribeLocalEvent<BlobCarrierComponent, MapInitEvent>(OnStartup);
        SubscribeLocalEvent<BlobCarrierComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<BlobCarrierComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<BlobCarrierComponent, MindRemovedMessage>(OnMindRemove);
    }

    [ValidatePrototypeId<EntityPrototype>]
    private const string ActionTransformToBlob = "ADTActionTransformToBlob";

    [ValidatePrototypeId<LanguagePrototype>]
    private const string BlobLang = "ADTBlob";

    private void OnRemove(Entity<BlobCarrierComponent> ent, ref ComponentRemove args)
    {
        if (TryComp<LanguageSpeakerComponent>(ent.Owner, out var comp))
            comp.Languages.Remove(BlobLang);
    }

    private void OnMindAdded(EntityUid uid, BlobCarrierComponent component, MindAddedMessage args) => component.HasMind = true;

    private void OnMindRemove(EntityUid uid, BlobCarrierComponent component, MindRemovedMessage args) => component.HasMind = false;

    private void OnTransformToBlobChanged(Entity<BlobCarrierComponent> uid, ref TransformToBlobActionEvent args)
    {
        if (!CanTransform())
        {
            _popup.PopupEntity(Loc.GetString("carrier-blob-too-early"), uid, uid, PopupType.LargeCaution);
            return;
        }

        TransformToBlob(uid);
    }

    private void OnStartup(EntityUid uid, BlobCarrierComponent component, MapInitEvent args)
    {
        _language.AddSpokenLanguage(uid, BlobLang);
        _action.AddAction(uid, ref component.TransformToBlob, ActionTransformToBlob);
        //EnsureComp<BlobSpeakComponent>(uid).OverrideName = false;

        if (HasComp<ActorComponent>(uid))
            return;

        var ghostRole = EnsureComp<GhostRoleComponent>(uid);
        EnsureComp<GhostTakeoverAvailableComponent>(uid);
        ghostRole.RoleName = Loc.GetString("blob-carrier-role-name");
        ghostRole.RoleDescription = Loc.GetString("blob-carrier-role-desc");
        ghostRole.RoleRules = Loc.GetString("blob-carrier-role-rules");
    }

    private void OnMobStateChanged(Entity<BlobCarrierComponent> uid, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead && CanTransform())
        {
            TransformToBlob(uid);
        }
    }

    protected override void TransformToBlob(Entity<BlobCarrierComponent> ent)
    {
        var xform = Transform(ent);
        if (!HasComp<MapGridComponent>(xform.GridUid))
            return;

        if (_mind.TryGetMind(ent, out _, out var mind) && mind.UserId != null)
        {
            var core = Spawn(ent.Comp.CoreBlobPrototype, xform.Coordinates);
            var ghostRoleComp = EnsureComp<GhostRoleComponent>(core);

            // Unfortunately we have to manually turn this off so we don't need to make more prototypes.
            _ghost.UnregisterGhostRole((core, ghostRoleComp));

            if (!TryComp<BlobCoreComponent>(core, out var blobCoreComponent))
                return;

            _blobCoreSystem.CreateBlobObserver(core, mind.UserId.Value, blobCoreComponent);
        }
        else
        {
            Spawn(ent.Comp.CoreBlobPrototype, xform.Coordinates);
        }

        _gibbingSystem.Gib(ent);
    }
}
