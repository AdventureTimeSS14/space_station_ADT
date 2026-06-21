using Content.Shared.ADT.Clothing.Wallet;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Wallet;

public sealed class WalletVisualizerSystem : VisualizerSystem<WalletVisualsComponent>
{
    protected override void OnAppearanceChange(
        EntityUid uid,
        WalletVisualsComponent comp,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(
                uid,
                WalletVisuals.StatusWallet,
                out var open,
                args.Component))
            return;

        SpriteSystem.LayerSetVisible(
            (uid, args.Sprite),
            WalletVisualLayers.Closed,
            !open);

        SpriteSystem.LayerSetVisible(
            (uid, args.Sprite),
            WalletVisualLayers.Open,
            open);
    }

    public enum WalletVisualLayers : byte
    {
        Closed,
        Open
    }
}
