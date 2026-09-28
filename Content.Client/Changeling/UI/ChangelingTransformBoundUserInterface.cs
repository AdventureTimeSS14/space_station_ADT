<<<<<<< HEAD
// ADT: Закомментировано из-за использования генокрада от Goob Station
// using Content.Client.Stylesheets.Palette;
// using Content.Client.UserInterface.Controls;
// using Content.Shared.Changeling.Components;
// using Content.Shared.Changeling.Systems;
// using JetBrains.Annotations;
// using Robust.Client.UserInterface;
=======
using Content.Client.Stylesheets.Palette;
using Content.Client.UserInterface.Controls;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;
>>>>>>> wizards-filtered

// namespace Content.Client.Changeling.UI;

<<<<<<< HEAD
// [UsedImplicitly]
// public sealed partial class ChangelingTransformBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
// {
//     private SimpleRadialMenu? _menu;
//     private static readonly Color SelectedOptionBackground = Palettes.Green.Element.WithAlpha(128);
//     private static readonly Color SelectedOptionHoverBackground = Palettes.Green.HoveredElement.WithAlpha(128);
=======
[UsedImplicitly]
public sealed partial class ChangelingTransformBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;
    private static readonly Color SelectedOptionBackground = Palettes.Green.Element.WithAlpha(128);
    private static readonly Color DisabledOptionBackground = Palettes.Slate.Element.WithAlpha(128);
    private static readonly Color SelectedOptionHoverBackground = Palettes.Green.HoveredElement.WithAlpha(128);
    private static readonly Color DisabledOptionHoverBackground = Palettes.Slate.HoveredElement.WithAlpha(128);
>>>>>>> wizards-filtered

//     protected override void Open()
//     {
//         base.Open();

//         _menu = this.CreateWindow<SimpleRadialMenu>();
//         Update();
//         _menu.OpenOverMouseScreenPosition();
//     }

//     public override void Update()
//     {
//         if (_menu == null)
//             return;

<<<<<<< HEAD
//         if (!EntMan.TryGetComponent<ChangelingIdentityComponent>(Owner, out var lingIdentity))
//             return;

//         var models = ConvertToButtons(lingIdentity.ConsumedIdentities.Keys, lingIdentity?.CurrentIdentity);
=======
        if (!EntMan.TryGetComponent<ChangelingIdentityComponent>(Owner, out var lingIdentity))
            return;
        
        var manualDrop = true;

        if (EntMan.TryGetComponent<ChangelingTransformComponent>(Owner, out var lingTransform))
            manualDrop = lingTransform.ManualDrop;
            
        var models = ConvertToButtons(lingIdentity.ConsumedIdentities, lingIdentity.CurrentIdentity, manualDrop);
>>>>>>> wizards-filtered

//         _menu.SetButtons(models);
//     }

<<<<<<< HEAD
//     private IEnumerable<RadialMenuOptionBase> ConvertToButtons(
//         IEnumerable<EntityUid> identities,
//         EntityUid? currentIdentity
//     )
//     {
//         var buttons = new List<RadialMenuOptionBase>();
//         foreach (var identity in identities)
//         {
//             if (!EntMan.TryGetComponent<MetaDataComponent>(identity, out var metadata))
//                 continue;

//             var option = new RadialMenuActionOption<NetEntity>(SendIdentitySelect, EntMan.GetNetEntity(identity))
//             {
//                 IconSpecifier = RadialMenuIconSpecifier.With(identity),
//                 ToolTip = metadata.EntityName,
//                 BackgroundColor = (currentIdentity == identity) ? SelectedOptionBackground : null,
//                 HoverBackgroundColor = (currentIdentity == identity) ? SelectedOptionHoverBackground : null
//             };
//             buttons.Add(option);
//         }
=======
    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(
        IEnumerable<ChangelingIdentityData> identities,
        EntityUid? currentIdentity,
        bool canDrop
    )
    {
        var buttons = new List<RadialMenuOptionBase>();
        var dropButtons = new List<RadialMenuOptionBase>();

        foreach (var identity in identities)
        {
            if (identity.Identity == null)
                continue;

            // Options for selecting identities.
            var option = new RadialMenuActionOption<NetEntity>(SendIdentitySelect, EntMan.GetNetEntity(identity.Identity.Value))
            {
                IconSpecifier = RadialMenuIconSpecifier.With(identity.Identity.Value),
                ToolTip = Loc.GetString("changeling-transform-bui-select-entity", ("entity", identity.Identity)),
                BackgroundColor = (currentIdentity == identity.Identity) ? SelectedOptionBackground : null, // mark as selected
                HoverBackgroundColor = (currentIdentity == identity.Identity) ? SelectedOptionHoverBackground : null
            };
            buttons.Add(option);

            if (!canDrop)
                continue;

            // Options for dropping identities.
            var dropOption = new RadialMenuActionOption<NetEntity>(SendIdentityDrop, EntMan.GetNetEntity(identity.Identity.Value))
            {
                IconSpecifier = RadialMenuIconSpecifier.With(identity.Identity.Value),
                ToolTip = (currentIdentity == identity.Identity)
                    ? Loc.GetString("changeling-transform-bui-drop-identity-cannot-drop")
                    : Loc.GetString("changeling-transform-bui-drop-identity-entity", ("entity", identity.Identity)),
                BackgroundColor = (currentIdentity == identity.Identity) ? DisabledOptionBackground : null, // cannot drop your current identity
                HoverBackgroundColor = (currentIdentity == identity.Identity) ? DisabledOptionHoverBackground : null
            };
            dropButtons.Add(dropOption);
        }
        
        if (canDrop)
        {
            // Menu category for dropping identities.
            var dropMenuButton = new RadialMenuNestedLayerOption(dropButtons)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/delete.svg.192dpi.png"))),
                ToolTip = Loc.GetString("changeling-transform-bui-drop-identity-menu")
            };
            buttons.Add(dropMenuButton);
        }
>>>>>>> wizards-filtered

//         return buttons;
//     }

<<<<<<< HEAD
//     private void SendIdentitySelect(NetEntity identityId)
//     {
//         SendPredictedMessage(new ChangelingTransformIdentitySelectMessage(identityId));
//     }
// }
=======
    private void SendIdentitySelect(NetEntity identityId)
    {
        SendPredictedMessage(new ChangelingTransformIdentitySelectMessage(identityId));
    }

    private void SendIdentityDrop(NetEntity identityId)
    {
        SendPredictedMessage(new ChangelingTransformIdentityDropMessage(identityId));
    }
}
>>>>>>> wizards-filtered
