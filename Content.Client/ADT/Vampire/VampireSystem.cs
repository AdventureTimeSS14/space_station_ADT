// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Client.Administration.Managers;
using Content.Client.Alerts;
using Content.Client.Ghost;
using Content.Shared.Administration;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.Vampire;

public sealed partial class VampireSystem : SharedVampireSystem
{
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private IClientAdminManager _admin = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, GetStatusIconsEvent>(GetVampireIcons);
        SubscribeLocalEvent<ThrallComponent, GetStatusIconsEvent>(GetThrallIcons);
        SubscribeLocalEvent<VampireComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);

        SubscribeLocalEvent<VampireDiablerieComponent, ComponentStartup>(OnDiablerieStartup);
        SubscribeLocalEvent<VampireDiablerieComponent, ComponentShutdown>(OnDiablerieShutdown);
        SubscribeLocalEvent<VampireDiablerieComponent, AfterAutoHandleStateEvent>(OnDiablerieStateUpdated);
    }

    // Okey, let's go
    private void GetVampireIcons(Entity<VampireComponent> ent, ref GetStatusIconsEvent args)
    {
        // If the local user is an admin in the ghost?
        if (_admin.HasFlag(AdminFlags.Admin) && _ghost is { IsGhost: true })
        {
            ShowIcon(ProtoMan.Index(ent.Comp.StatusIcon), ref args);
            return;
        }

        // --- Admins ignore this above and see all vampires ---
        // If he's not the owner of the thralls, we will not see the icon
        if (!HasComp<ThrallOwnerComponent>(ent))
            return;

        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == ent.Owner) // Is that you?
        {
            ShowIcon(ProtoMan.Index(ent.Comp.StatusIcon), ref args);
            return;
        }

        // If we're a vampire's servant?
        if (TryComp<ThrallComponent>(localPlayer, out var thrall) && thrall.VampireOwner == ent.Owner)
        {
            ShowIcon(ProtoMan.Index(ent.Comp.StatusIcon), ref args);
            return;
        }
    }

    private void GetThrallIcons(Entity<ThrallComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<VampireComponent>(ent))
            return;

        // If the local user is an admin in the ghost?
        if (_admin.HasFlag(AdminFlags.Admin) && _ghost is { IsGhost: true })
        {
            ShowIcon(ProtoMan.Index(ent.Comp.StatusIcon), ref args);
            return;
        }

        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == ent.Owner) // Is that you?
        {
            ShowIcon(ProtoMan.Index(ent.Comp.StatusIcon), ref args);
            return;
        }

        // If we are the vampire owner of this servant?
        if (ent.Comp.VampireOwner == localPlayer)
        {
            ShowIcon(ProtoMan.Index(ent.Comp.StatusIcon), ref args);
            return;
        }

        // If we were another servant of the same vampire owner?
        if (TryComp<ThrallComponent>(localPlayer, out var localThrall)
            && localThrall.VampireOwner == ent.Comp.VampireOwner)
        {
            ShowIcon(ProtoMan.Index(ent.Comp.StatusIcon), ref args);
            return;
        }
    }

    private void ShowIcon(StatusIconPrototype icon, ref GetStatusIconsEvent args)
        => args.StatusIcons.Add(icon);

    private void OnUpdateAlert(Entity<VampireComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.BloodAlert)
            return;

        var blood = Math.Clamp(ent.Comp.CurrentBlood.Int(), 0, 9999);

        _sprite.LayerSetRsiState(args.SpriteViewEnt.Owner, VampireVisualLayers.Digit1, $"{(blood / 1000) % 10}");
        _sprite.LayerSetRsiState(args.SpriteViewEnt.Owner, VampireVisualLayers.Digit2, $"{(blood / 100) % 10}");
        _sprite.LayerSetRsiState(args.SpriteViewEnt.Owner, VampireVisualLayers.Digit3, $"{(blood / 10) % 10}");
        _sprite.LayerSetRsiState(args.SpriteViewEnt.Owner, VampireVisualLayers.Digit4, $"{blood % 10}");
    }

    private void OnDiablerieStartup(Entity<VampireDiablerieComponent> ent, ref ComponentStartup args)
        => UpdateDiablerieAura(ent);

    private void OnDiablerieShutdown(Entity<VampireDiablerieComponent> ent, ref ComponentShutdown args)
        => RemoveDiablerieAura(ent);

    private void OnDiablerieStateUpdated(Entity<VampireDiablerieComponent> ent, ref AfterAutoHandleStateEvent args)
        => UpdateDiablerieAura(ent);

    private void UpdateDiablerieAura(Entity<VampireDiablerieComponent> ent)
    {
        if (!HasComp<SpriteComponent>(ent))
            return;

        // If the local user is an admin in the ghost?
        if (_admin.HasFlag(AdminFlags.Admin) && _ghost is { IsGhost: true })
        {
            AddOrUpdateAuraLayer(ent);
            return;
        }

        var localPlayer = _playerManager.LocalEntity;
        var isLocalPlayer = localPlayer == ent.Owner;
        var isExaminerVampire = localPlayer != null && HasComp<VampireComponent>(localPlayer);
        var level = ent.Comp.DiablerieLevel;

        // Level 1+ aura visible to vampires
        // Level 3+ aura visible to everyone
        var canSeeAura = level >= 1 && isExaminerVampire || level >= 3;

        // Self visibility
        if (isLocalPlayer && level >= 1)
            canSeeAura = true;

        if (!canSeeAura)
        {
            RemoveDiablerieAura(ent);
            return;
        }

        AddOrUpdateAuraLayer(ent);
    }

    private void AddOrUpdateAuraLayer(Entity<VampireDiablerieComponent> ent)
    {
        if (!_sprite.LayerMapTryGet(ent.Owner, DiablerieKey.Aura, out var layer, true))
        {
            var layerData = new PrototypeLayerData
            {
                RsiPath = "/Textures/ADT/Vampire/Interface/Misc/vampire_aura.rsi",
                State = "diablerie_aura"
            };

            layer = _sprite.AddLayer(ent.Owner, layerData, null);
            _sprite.LayerMapSet(ent.Owner, DiablerieKey.Aura, layer);
        }

        _sprite.LayerSetVisible(ent.Owner, layer, true);
    }

    private void RemoveDiablerieAura(Entity<VampireDiablerieComponent> ent)
    {
        if (!HasComp<SpriteComponent>(ent))
            return;

        if (_sprite.LayerMapTryGet(ent.Owner, DiablerieKey.Aura, out var layer, true))
            _sprite.RemoveLayer(ent.Owner, layer);
    }

    private enum DiablerieKey
    {
        Aura
    }
}
