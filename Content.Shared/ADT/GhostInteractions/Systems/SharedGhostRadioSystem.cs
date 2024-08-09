using Content.Shared.Examine;
using Content.Shared.Toggleable;

namespace Content.Shared.ADT.GhostInteractions;

public abstract class SharedGhostRadioSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostRadioComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, GhostRadioComponent component, ExaminedEvent args)
    {
        var state = Loc.GetString(component.Enabled
            ? "ghost-radio-enabled"
            : "ghost-radio-disabled");

        args.PushMarkup(state);
    }

    protected void OnAppearanceChange(EntityUid radio, GhostRadioComponent? comp = null)
    {
        if (comp == null && !TryComp(radio, out comp))
            return;

        _appearance.SetData(radio, ToggleVisuals.Toggled, comp.Enabled);
    }
}
