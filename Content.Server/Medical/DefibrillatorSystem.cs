using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Traits.Assorted;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.ADT.Atmos.Miasma;
using Robust.Server.Containers;
using System.Linq;
using Content.Server.Resist; //ADT-Medicine

namespace Content.Server.Medical;

public sealed class DefibrillatorSystem : SharedDefibrillatorSystem
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    protected override void OpenReturnToBodyEui(Entity<MindComponent> mind, ICommonSession session)
    {
        _eui.OpenEui(new ReturnToBodyEui(mind, _mind, _player), session);
    }
}
