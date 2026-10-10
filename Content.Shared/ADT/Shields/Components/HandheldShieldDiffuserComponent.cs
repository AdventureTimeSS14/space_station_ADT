using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Shared.ADT.Shields;

[RegisterComponent]
public sealed partial class HandheldShieldDiffuserComponent : Component
{
    [DataField] public bool Enabled;

    [DataField] public float ActivePowerUse = 5f;

    [DataField] public float DiffuseDuration = 20f;

    [DataField] public int ActiveRadius = 2;

    public HashSet<(EntityUid Grid, Vector2i Tile)> SuppressedTiles = new();

    public EntityUid? LastGrid;
    public Vector2i LastTile;
    public bool HasLastPosition;

    public TimeSpan NextProcess;
}