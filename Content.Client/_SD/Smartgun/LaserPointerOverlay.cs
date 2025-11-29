
using Content.Shared._SD.Weapons.SmartGun;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Numerics;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content._SD.Client.Weapons.LaserPointer;

public sealed class LaserPointerOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    private readonly IEntityManager _entManager;

    private readonly TransformSystem _transform;

    private readonly ShaderInstance _unshadedShader;

    public LaserPointerOverlay(IEntityManager entManager, IPrototypeManager prototype)
    {
        ZIndex = (int) DrawDepth.Effects;

        _entManager = entManager;

        _transform = entManager.System<TransformSystem>();

        _unshadedShader = prototype.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var bounds = args.WorldAABB;
        var currentMapId = args.MapId;

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var query = _entManager.EntityQueryEnumerator<LaserPointerManagerComponent>();
        handle.UseShader(_unshadedShader);
        while (query.MoveNext(out var manager))
        {
            foreach (var (netEnt, data) in manager.Data)
            {
                var start = data.Start;
                var end = data.End;

                var ent = _entManager.GetEntity(netEnt);
                if (xformQuery.TryComp(ent, out var xform))
                {
                    var coords = _transform.GetMapCoordinates(ent, xform);

                    if (coords.MapId != currentMapId)
                        continue;

                    if (coords.MapId != MapId.Nullspace)
                        start = coords.Position;
                }

                var (left, right) = MinMax(start.X, end.X);
                var (bottom, top) = MinMax(start.Y, end.Y);

                if (!bounds.Intersects(new Box2(left, bottom, right, top)))
                    continue;

                DrawWideLine(handle, start, end, data.Color, 0.025f);
            }
        }

        handle.UseShader(null);
    }

    private void DrawWideLine(DrawingHandleWorld handle, Vector2 start, Vector2 end, Color color, float width)
    {
        var direction = end - start;
        var length = direction.Length();

        // Если линия очень короткая, пропускаем
        if (length < 0.001f)
            return;

        var normalizedDir = direction / length;

        // Перпендикулярный вектор для создания ширины
        var perpendicular = new Vector2(-normalizedDir.Y, normalizedDir.X) * width / 2f;

        // Создаем 4 вершины прямоугольника
        var a = start - perpendicular;
        var b = start + perpendicular;
        var c = end + perpendicular;
        var d = end - perpendicular;

        // Определяем вершины для двух треугольников (образующих прямоугольник)
        var vertices = new[]
        {
            a, b, c, // Первый треугольник
            a, c, d  // Второй треугольник
        };

        // Рисуем треугольники
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, vertices, color.WithAlpha(35));
    }

    private static (float min, float max) MinMax(float a, float b)
    {
        return a >= b ? (b, a) : (a, b);
    }
}
