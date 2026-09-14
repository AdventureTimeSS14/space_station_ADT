namespace Content.Goobstation.Shared.Changeling.Systems;

/// <summary>
/// Shared base for Void Adaption. The actual immunity handling lives server-side in
/// <c>VoidAdaptionSystem</c>, which attaches the base immunity components when the ability is gained.
/// </summary>
public abstract class SharedVoidAdaptionSystem : EntitySystem
{
}
