// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), AGPL-3.0-or-later.
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Atmos;

/// <summary>
/// Сервер просит всех вокруг проиграть анимацию катания по полу.
/// </summary>
[Serializable, NetSerializable]
public sealed class RMCStopDropRollVisualsNetworkEvent(NetEntity user, TimeSpan length) : EntityEventArgs
{
    public readonly NetEntity User = user;

    /// <summary>
    /// Сколько катаемся. Совпадает с длительностью тушения, чтобы анимация не жила дольше самого действия.
    /// </summary>
    public readonly TimeSpan Length = length;
}
