// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Addiction;

/// <summary>
/// Зависимость для анализатора здоровья. Kind - id AddictionPrototype.
/// </summary>
[Serializable, NetSerializable]
public struct AddictionInfo
{
    public string Kind;
    public int Stage;
    public bool Permanent;

    public AddictionInfo(string kind, int stage, bool permanent)
    {
        Kind = kind;
        Stage = stage;
        Permanent = permanent;
    }
}
