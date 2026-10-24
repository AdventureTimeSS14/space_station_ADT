namespace Content.Server.Storage.Components
{
    /// <summary>
    ///     Allows forcing a single prototype id to be spawned by a SpawnItemsOnUse component.
    ///     The field is editable in-game via ViewVariables.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SpawnItemSelectorComponent : Component
    {
        /// <summary>
        /// Prototype id to spawn when used.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("selectedItemId")]
        public string? SelectedItemId = null;
    }
}
