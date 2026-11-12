using Content.Server.Chat.Systems;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Robust.Shared.Random;

namespace Content.Server.ADT.Virology;

public sealed partial class ADTDiseaseSystem
{
    public const int MasterStageTime = 60;

    public const int MasterActiveStage = 4;

    private void BindMaster(EntityUid uid, ADTDisease disease, ADTDiseasePrototype proto)
    {
        if (!proto.BindsMaster)
            return;

        if (disease.Master != null && TryGetEntity(disease.Master.Value, out var existing) && !Deleted(existing))
            return;

        disease.Master = GetNetEntity(uid);
        disease.MasterTimer = 0;
    }

    private void MasterAct(Entity<ADTDiseaseCarrierComponent> ent, ADTDisease disease)
    {
        if (disease.Stage < MasterActiveStage || disease.Master == null)
            return;

        if (!TryGetEntity(disease.Master.Value, out var master) || Deleted(master))
            return;

        if (master.Value == ent.Owner)
            return;

        if (_mobState.IsDead(master.Value))
        {
            LoseMaster(ent, disease, master.Value);
            return;
        }

        var name = Identity.Name(master.Value, EntityManager);
        var sees = _examine.InRangeUnOccluded(ent.Owner, master.Value, 8f);

        if (sees)
        {
            disease.MasterTimer = Math.Max(disease.MasterTimer - (disease.MasterTimer / 10 + 1), 0);

            if (_random.Prob(0.01f))
            {
                Say(ent, Loc.GetString(_random.Pick(MasterAdmireLines), ("name", name)));
                _statusEffects.TryAddStatusEffectDuration(ent.Owner, "StatusEffectKnockdown", TimeSpan.FromSeconds(1));
            }
            else if (_random.Prob(0.05f))
            {
                Say(ent, Loc.GetString(_random.Pick(MasterPresentLines), ("name", name)));
            }

            return;
        }

        disease.MasterTimer++;

        var stage = disease.MasterTimer switch
        {
            <= MasterStageTime => 0,
            <= MasterStageTime * 2 => 1,
            <= MasterStageTime * 4 => 2,
            _ => 3,
        };

        if (_random.Prob(0.05f))
        {
            var lines = stage switch
            {
                0 => MasterMissSoftLines,
                1 => MasterMissLines,
                2 => MasterMissHardLines,
                _ => MasterMissDyingLines,
            };

            Say(ent, Loc.GetString(_random.Pick(lines), ("name", name)));
        }

        var damage = stage switch
        {
            0 => 0f,
            1 => 0.25f,
            2 => 0.66f,
            _ => Math.Min(disease.MasterTimer / (4f * MasterStageTime), 4f),
        };

        if (damage <= 0f)
            return;

        _damage.TryChangeDamage(ent.Owner, new DamageSpecifier(_proto.Index<DamageTypePrototype>("Poison"), damage), true);
    }

    private void LoseMaster(Entity<ADTDiseaseCarrierComponent> ent, ADTDisease disease, EntityUid master)
    {
        var name = Identity.Name(master, EntityManager);

        _popup.PopupEntity(Loc.GetString("adt-disease-loyalty-master-dead", ("name", name)), ent.Owner, ent.Owner, Shared.Popups.PopupType.LargeCaution);
        _chat.TryEmoteWithChat(ent.Owner, "Scream", ignoreActionBlocker: true, forceEmote: true);
        _damage.TryChangeDamage(ent.Owner, new DamageSpecifier(_proto.Index<DamageTypePrototype>("Poison"), 20f), true);

        Cure(ent.AsNullable(), disease, needImmunity: false);
    }

    private void Say(Entity<ADTDiseaseCarrierComponent> ent, string message)
    {
        _chat.TrySendInGameICMessage(ent.Owner, message, InGameICChatType.Speak, hideChat: false, hideLog: true);
    }

    private static readonly string[] MasterAdmireLines =
    {
        "adt-disease-loyalty-admire-1",
        "adt-disease-loyalty-admire-2",
        "adt-disease-loyalty-admire-3",
    };

    private static readonly string[] MasterPresentLines =
    {
        "adt-disease-loyalty-present-1",
        "adt-disease-loyalty-present-2",
        "adt-disease-loyalty-present-3",
    };

    private static readonly string[] MasterMissSoftLines =
    {
        "adt-disease-loyalty-miss-soft-1",
        "adt-disease-loyalty-miss-soft-2",
    };

    private static readonly string[] MasterMissLines =
    {
        "adt-disease-loyalty-miss-1",
        "adt-disease-loyalty-miss-2",
    };

    private static readonly string[] MasterMissHardLines =
    {
        "adt-disease-loyalty-miss-hard-1",
        "adt-disease-loyalty-miss-hard-2",
    };

    private static readonly string[] MasterMissDyingLines =
    {
        "adt-disease-loyalty-miss-dying-1",
        "adt-disease-loyalty-miss-dying-2",
    };
}
