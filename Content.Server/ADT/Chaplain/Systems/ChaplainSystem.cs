using Content.Server.Chaplain;
using Content.Server.EUI;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.Chaplain.Systems;
using Content.Shared.Mind;
using Robust.Shared.Player;

namespace Content.Server.ADT.Chaplain;

public sealed class ChaplainSystem : SharedChaplainSystem
{
    [Dependency] private EuiManager _euiManager = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    protected override void OfferReligion(Entity<ChaplainComponent> ent, EntityUid target)
    {
        if (!_mind.TryGetMind(target, out _, out var mind) || !_player.TryGetSessionById(mind.UserId, out var session))
            return;

        _euiManager.OpenEui(new AcceptReligionEui(ent, target, this), session);
    }
}
