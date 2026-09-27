using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Shared.ADT.Shields;

/// <summary>Напольный рассеиватель: разгоняет щит крестом вокруг себя.</summary>
[RegisterComponent]
public sealed partial class ShieldDiffuserComponent : Component
{
    [DataField] public bool Enabled = true;

    [DataField] public float Alarm;

    [DataField] public float DiffuseRefresh = 5f;

    public HashSet<(EntityUid Grid, Vector2i Tile)> DiffusedTiles = new();
}