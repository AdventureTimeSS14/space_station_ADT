using Content.Shared.Construction.Components;
using Content.Shared.ADT.Anchored.Components;

namespace Content.Server.ADT.Anchored

{
    public sealed class AnchoredVisualsSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AnchorableComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<AnchorableComponent, AnchorStateChangedEvent>(OnAnchorChange);
        }

        private void OnStartup(EntityUid uid, AnchorableComponent component, ComponentStartup args)
        {
            UpdateAnchored(uid, component, Transform(uid).Anchored);
        }

        private void OnAnchorChange(EntityUid uid, AnchorableComponent component, ref AnchorStateChangedEvent args)
        {
            UpdateAnchored(uid, component, args.Anchored);
        }

        private void UpdateAnchored(EntityUid uid, AnchorableComponent component, bool anchored)
        {
            if (anchored)
            {
                _appearanceSystem.SetData(uid, AnchoredVisuals.VisualState, AnchoredVisualState.Anchored);
            }
            else
            {
                _appearanceSystem.SetData(uid, AnchoredVisuals.VisualState, AnchoredVisualState.Free);
            }
        }
    }
}
