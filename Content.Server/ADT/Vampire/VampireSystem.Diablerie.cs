// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Shared.Administration.Systems;
using Content.Shared.ADT.Vampire;
using Content.Shared.ADT.Vampire.Components;
using Content.Shared.Bible.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private RejuvenateSystem _rejuvenate = default!;
    [Dependency] private SharedChargesSystem _charges = default!;

    private static readonly EntProtoId Ash = "Ash";

    private void InitializeDiablerie()
    {
        SubscribeLocalEvent<VampireDiablerieComponent, ComponentStartup>(OnDiablerieStartup);
        SubscribeLocalEvent<VampireDiablerieComponent, ExaminedEvent>(OnDiablerieExamined);
        SubscribeLocalEvent<VampireDiablerieComponent, VampireSacramentInitiationActionEvent>(OnSacramentInitiation);
        SubscribeLocalEvent<VampireDiablerieComponent, MeleeHitEvent>(OnDiablerieMeleeHit);
    }

    private void OnDiablerieMeleeHit(Entity<VampireDiablerieComponent> ent, ref MeleeHitEvent args)
    {
        if (ent.Comp.DiablerieLevel < 4 || args.HitEntities.Count == 0)
            return;

        args.BonusDamage += new DamageSpecifier
        {
            DamageDict = { { "Blunt", 5 } }
        };
    }

    private void OnDiablerieStartup(Entity<VampireDiablerieComponent> ent, ref ComponentStartup args)
        => ApplyDiablerieBonuses(ent);

    private void OnDiablerieExamined(EntityUid uid, VampireDiablerieComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var name = Identity.Name(uid, EntityManager, args.Examiner);

        if (component.DiablerieLevel >= 1 && HasComp<VampireComponent>(args.Examiner) || component.DiablerieLevel >= 2)
            args.PushMarkup(Loc.GetString("vampire-diablerie-aura-visible", ("name", name)));

        if (component.DiablerieLevel >= 2)
        {
            bool eyesCovered = false;
            var clothes = _inventory.GetSlotEnumerator(uid, SlotFlags.WITHOUT_POCKET);
            while (clothes.NextItem(out var cloth, out _))
            {
                if (TryComp<IdentityBlockerComponent>(cloth, out var blocker) && blocker.Coverage.HasFlag(IdentityBlockerCoverage.EYES)
                    && blocker.Enabled)
                {
                    eyesCovered = true;
                    break;
                }
            }

            if (!eyesCovered)
            {
                args.PushMarkup(Loc.GetString("vampire-diablerie-eyes-glow-examined", ("name", name)));
            }
        }
    }

    private void OnSacramentInitiation(Entity<VampireDiablerieComponent> ent, ref VampireSacramentInitiationActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var target = args.Target;
        if (ent.Owner == target)
            return;

        if (!_mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-initiation-failed"), ent, ent, PopupType.SmallCaution);
            return;
        }

        if (!HasComp<HumanoidProfileComponent>(target) || HasComp<VampireComponent>(target) || HasComp<MindShieldComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-initiation-failed"), ent, ent, PopupType.SmallCaution);
            return;
        }

        if (HasComp<BibleUserComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-initiation-failed"), ent, ent, PopupType.SmallCaution);
            return;
        }

        _rejuvenate.PerformRejuvenate(target);

        EnsureComp<VampireInferiorComponent>(target);
        var vampire = EnsureComp<VampireComponent>(target);
        var state = EnsureComp<VampireOriginalStateComponent>(target);
        SaveOriginalState((target, vampire), state);

        MakeVampire((target, vampire), true);

        _antag.SendBriefing(target, Loc.GetString("free-vampire-greeting"), Color.Purple,
            new SoundPathSpecifier("/Audio/ADT/Vampire/Ambience/Antag/vampare_start.ogg"));

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private bool TryPerformDiablerie(Entity<VampireComponent> vampire, Entity<VampireComponent> targetVampire, FixedPoint2 volumeToEssence)
    {
        if (vampire.Comp.CurrentEvolution == VampireClassEnum.NonSelected)
        {
            _popup.PopupEntity(Loc.GetString("vampire-diablerie-no-class"), vampire, vampire, PopupType.SmallCaution);
            return false;
        }

        var bloodToDrain = FixedPoint2.Min(targetVampire.Comp.CurrentBlood, volumeToEssence);
        SubtractBloodEssence(targetVampire.Owner, bloodToDrain);

        if (targetVampire.Comp.CurrentBlood <= 0)
        {
            CompleteDiablerie(vampire, targetVampire);
            return false;
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("vampire-diablerie-draining",
                ("target", Identity.Name(targetVampire, EntityManager))), vampire, vampire);

            _popup.PopupEntity(Loc.GetString("vampire-diablerie-target-draining"), targetVampire, targetVampire);
        }

        return true;
    }

    private void CompleteDiablerie(Entity<VampireComponent> vampire, Entity<VampireComponent> targetVampire)
    {
        if (targetVampire.Comp.CurrentEvolution == VampireClassEnum.NonSelected)
        {
            _popup.PopupEntity(Loc.GetString("vampire-diablerie-target-no-class"), vampire, vampire, PopupType.MediumCaution);
            return;
        }

        var targetName = Identity.Name(targetVampire, EntityManager);
        _popup.PopupCoordinates(Loc.GetString("vampire-diablerie-dust", ("target", targetName)),
            Transform(targetVampire).Coordinates, PopupType.Large);

        _admin.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(vampire)} performed diablerie on {ToPrettyString(targetVampire)}");

        var diablerie = EnsureComp<VampireDiablerieComponent>(vampire);
        IncreaseDiablerieLevel((vampire, diablerie));

        Spawn(Ash, Transform(targetVampire).Coordinates);
        foreach (var item in _inventory.GetHandOrInventoryEntities(targetVampire.Owner))
            _transform.DropNextTo(item, targetVampire.Owner);

        QueueDel(targetVampire);
    }

    private void IncreaseDiablerieLevel(Entity<VampireDiablerieComponent> ent)
    {
        if (ent.Comp.DiablerieLevel >= ent.Comp.MaxDiablerieLevel)
            return;

        ent.Comp.DiablerieLevel++;

        ApplyDiablerieBonuses(ent);
        Dirty(ent.Owner, ent.Comp);
    }

    private void ApplyDiablerieBonuses(Entity<VampireDiablerieComponent> ent, VampireComponent? vampire = null)
    {
        if (!Resolve(ent.Owner, ref vampire, false))
            return;

        var level = ent.Comp.DiablerieLevel;

        switch (level)
        {
            case 1:
                {
                    if (vampire.RejuvenateActionEntity != null)
                    {
                        _charges.SetMaxCharges(vampire.RejuvenateActionEntity.Value, 2);
                    }
                    _popup.PopupEntity(Loc.GetString("vampire-diablerie-level-one"), ent, ent, PopupType.Medium);
                    break;
                }
            case 2:
                {
                    if (vampire.GlareActionEntity != null)
                    {
                        _charges.SetMaxCharges(vampire.GlareActionEntity.Value, 3);
                    }
                    _popup.PopupEntity(Loc.GetString("vampire-diablerie-level-two"), ent, ent, PopupType.Medium);
                    break;
                }
            case 3:
                {
                    AnnounceVampireAscended(ent);
                    GrantSacramentInitiationAbility(ent);
                    _popup.PopupEntity(Loc.GetString("vampire-diablerie-level-three"), ent, ent, PopupType.Large);
                    break;
                }
            case 4:
                {
                    _popup.PopupEntity(Loc.GetString("vampire-diablerie-level-four"), ent, ent, PopupType.Large);
                    break;
                }
        }

        if (level > 0)
            ApplyCooldownReduction((ent.Owner, vampire), level);
    }

    private void AnnounceVampireAscended(Entity<VampireDiablerieComponent> ent)
    {
        _chat.DispatchGlobalAnnouncement(Loc.GetString("vampire-ascended-announcement", ("name", Name(ent))), colorOverride: Color.Red);
        _admin.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(ent)} has reached maximum diablerie level ({ent.Comp.MaxDiablerieLevel})");
    }

    private void GrantSacramentInitiationAbility(Entity<VampireDiablerieComponent> ent)
    {
        if (ent.Comp.SacramentInitiationActionEntity != null)
            return;

        ent.Comp.SacramentInitiationActionEntity = _action.AddAction(ent.Owner,
            VampireDiablerieComponent.SacramentInitiationActionPrototype);
    }
}
