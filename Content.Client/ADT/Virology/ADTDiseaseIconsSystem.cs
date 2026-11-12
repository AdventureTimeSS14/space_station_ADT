using Content.Client.Overlays;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Virology;

public sealed class ADTDiseaseIconsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ShowHealthIconsSystem _healthIcons = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDiseaseVisualsComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnGetStatusIcons(Entity<ADTDiseaseVisualsComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!_healthIcons.IsActive)
            return;

        var id = ent.Comp.Severity switch
        {
            ADTDiseaseSeverity.Positive => "ADTDiseaseIconPositive",
            ADTDiseaseSeverity.Nonthreat => "ADTDiseaseIconNonthreat",
            ADTDiseaseSeverity.Minor => "ADTDiseaseIconMinor",
            ADTDiseaseSeverity.Medium => "ADTDiseaseIconMedium",
            ADTDiseaseSeverity.Harmful => "ADTDiseaseIconHarmful",
            ADTDiseaseSeverity.Dangerous => "ADTDiseaseIconDangerous",
            ADTDiseaseSeverity.Biohazard => "ADTDiseaseIconBiohazard",
            ADTDiseaseSeverity.Uncurable => "ADTDiseaseIconUncurable",
            _ => null,
        };

        if (id == null || !_proto.TryIndex<ADTDiseaseIconPrototype>(id, out var icon))
            return;

        args.StatusIcons.Add(icon);
    }
}
