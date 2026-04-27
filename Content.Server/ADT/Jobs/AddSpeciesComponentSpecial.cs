using Content.Shared.Humanoid;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Jobs;

public sealed partial class AddSpeciesComponentSpecial : JobSpecial
{
    [DataField(required: true)]
    public ComponentRegistry Components { get; private set; } = new();

    /// <summary>
    /// Species ID
    /// </summary>
    [DataField(required: true)]
    public string Species { get; private set; } = string.Empty;

    /// <summary>
    /// If this is true then existing components will be removed and replaced with these ones.
    /// </summary>
    [DataField]
    public bool RemoveExisting = true;

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();

        if (!entMan.TryGetComponent<HumanoidAppearanceComponent>(mob, out var humanoid))
            return;

        if (humanoid.Species != Species)
            return;

        entMan.AddComponents(mob, Components, removeExisting: RemoveExisting);
    }
}
