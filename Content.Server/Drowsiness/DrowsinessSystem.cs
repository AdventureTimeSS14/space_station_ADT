using Content.Shared.Drowsiness;
using Content.Shared.StatusEffectNew;

namespace Content.Server.Drowsiness;

/// <summary>
/// ADT-Tweak OPTIMIZATION
/// Server-specific extensions for drowsiness system.
/// Handles event subscriptions for status effects.
/// </summary>
public class DrowsinessSystem : SharedDrowsinessSystem
{
    public override void Initialize()
    {
        base.Initialize(); // ADT-Tweak OPTIMIZATION

        SubscribeLocalEvent<DrowsinessStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectAppliedHandler);
    }

    private void OnEffectAppliedHandler(Entity<DrowsinessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        // ADT-Tweak OPTIMIZATION: Call the base method from SharedDrowsinessSystem to initialize timer
        OnEffectApplied(ent, ref args);
    }
}
