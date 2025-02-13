using Content.Client.Interactable.Components;
using Content.Client.StatusIcon;
using Content.Shared.ADT.Stealth.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Stealth;

public sealed partial class StealthSystem
{
    private void InitializeADT()
    {
        _player.LocalPlayerAttached += OnAttachedChanged;
        _player.LocalPlayerDetached += OnAttachedChanged;
    }

    private void OnAttachedChanged(EntityUid uid)
    {
        var query = AllEntityQuery<StealthComponent, DigitalCamouflageComponent>();
        while (query.MoveNext(out var ent, out var comp, out _))
        {
            SetShader(ent, comp.Enabled);
        }
    }
}
