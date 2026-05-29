using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Body.Allergies.Prototypes;

/// <summary>
/// Прототип аллергической реакции.
/// 
/// Определяет, какие эффекты будут назначены при наступлении события аллергического шока,
/// если стак аллергической реакции у энтити >= StackThreshold.
/// </summary>
[Prototype("allergicReaction")]
public sealed partial class AllergicReactionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("effects")]
    public EntityEffect[] Effects = default!;

    [DataField("threshold", required: true)]
    public float StackThreshold;
}
