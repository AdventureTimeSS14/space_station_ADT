using Content.Shared.ADT.Legion;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Legion;

public sealed class ADTLegionMegafaunaVisualsSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private const string SpinKey = "adt-legion-spin";
    private const float QuarterTurn = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLegionMegafaunaComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<ADTLegionMegafaunaComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData<TimeSpan>(ent, ADTLegionMegafaunaVisuals.SpinUntil, out var until, args.Component))
            return;

        if (until == ent.Comp.ClientSpinUntil)
            return;

        ent.Comp.ClientSpinUntil = until;

        var remaining = (float) (until - _timing.CurTime).TotalSeconds;

        if (remaining <= 0f)
            return;

        var player = EnsureComp<AnimationPlayerComponent>(ent);

        if (_animation.HasRunningAnimation(player, SpinKey))
            _animation.Stop((ent.Owner, player), SpinKey);

        var track = new AnimationTrackComponentProperty
        {
            ComponentType = typeof(SpriteComponent),
            Property = nameof(SpriteComponent.Rotation),
            InterpolationMode = AnimationInterpolationMode.Linear,
        };

        var quarters = (int) MathF.Ceiling(remaining / QuarterTurn);
        track.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(Angle.Zero, 0f));

        for (var i = 1; i <= quarters; i++)
        {
            track.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(-90 * i), QuarterTurn));
        }

        track.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(Angle.Zero, 0f));

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(quarters * QuarterTurn),
            AnimationTracks = { track },
        };

        _animation.Play((ent.Owner, player), animation, SpinKey);
    }
}
