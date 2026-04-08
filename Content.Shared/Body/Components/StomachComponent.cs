using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(StomachSystem))]
public sealed partial class StomachComponent : Component
{
<<<<<<< HEAD
    [RegisterComponent, NetworkedComponent, Access(typeof(StomachSystem))]
    public sealed partial class StomachComponent : Component
    {
        /// <summary>
        ///     The next time that the stomach will try to digest its contents.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextUpdate;
=======
    /// <summary>
    /// The solution inside of this stomach
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution;
>>>>>>> upstreamwiz/master

    /// <summary>
    ///     A whitelist for what special-digestible-required foods this stomach is capable of eating.
    /// </summary>
    [DataField]
    public EntityWhitelist? SpecialDigestible = null;

    /// <summary>
    /// Controls whitelist behavior. If true, this stomach can digest <i>only</i> food that passes the whitelist. If false, it can digest normal food <i>and</i> any food that passes the whitelist.
    /// </summary>
    [DataField]
    public bool IsSpecialDigestibleExclusive = true;
}
