using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Client.Guidebook.Controls;
using Content.Client.Guidebook.Richtext;
using Content.Client.Resources;
using Content.Shared.ADT.Xenobiology;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Guidebook.Xenobiology;

public sealed class GuideXenobiologyTreeEmbed : Control, IDocumentTag, ISearchableControl
{
    private const string RootBreed = "GreyMutation";
    private const string SlimeRsi = "/Textures/ADT/Xenobiology/Mobs/slimesBaby.rsi";
    private const string SlimeState = "base";

    private const float NodeSize = 48f;
    private const float GreySize = 64f;

    private static readonly float[] RingRadii = { 0f, 85f, 148f, 210f, 272f };
    private const float CanvasSize = (272f + NodeSize / 2f + 8f) * 2f;

    private static readonly Dictionary<string, float> BreedAngles = new()
    {
        // Первый тир
        ["OrangeMutation"] = 45f,
        ["PurpleMutation"] = 135f,
        ["BlueMutation"] = 225f,
        ["MetalMutation"] = 315f,
        // Второй тир
        ["YellowMutation"] = 0f,
        ["DarkPurpleMutation"] = 90f,
        ["DarkBlueMutation"] = 180f,
        ["SilverMutation"] = 270f,
        // Второй тир
        ["RedMutation"] = 45f,
        ["GreenMutation"] = 135f,
        ["PinkMutation"] = 225f,
        ["GoldMutation"] = 315f,
        // Третий тир
        ["BluespaceMutation"] = 0f,
        ["SepiaMutation"] = 90f,
        ["CeruleanMutation"] = 180f,
        ["PyriteMutation"] = 270f,
        ["OilMutation"] = 45f,
        ["BlackMutation"] = 135f,
        ["LightPinkMutation"] = 225f,
        ["AdamantineMutation"] = 315f,
    };

    private readonly IPrototypeManager _proto;
    private readonly Font _font;
    private readonly Texture _slimeTexture;

    private readonly List<BreedNode> _nodes = new();
    private readonly List<(BreedNode Parent, BreedNode Child)> _edges = new();
    private BreedNode? _hoveredNode;

    private sealed record BreedNode(ProtoId<BreedPrototype> Id, BreedPrototype Breed, int Tier, Vector2 Pos, float Radius);

    public GuideXenobiologyTreeEmbed()
    {
        IoCManager.InjectDependencies(this);
        _proto = IoCManager.Resolve<IPrototypeManager>();

        var resource = IoCManager.Resolve<IResourceCache>();
        _font = resource.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 11);

        if (resource.TryGetResource<RSIResource>(new ResPath(SlimeRsi), out var rsi)
            && rsi.RSI.TryGetState(SlimeState, out var state))
        {
            _slimeTexture = state.Frame0;
        }
        else
        {
            _slimeTexture = null!;
        }

        MouseFilter = MouseFilterMode.Stop;
    }

    public bool CheckMatchesSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        return _nodes.Any(node => Loc.GetString(node.Breed.BreedName).Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    public void SetHiddenState(bool state, string query)
    {
        Visible = CheckMatchesSearch(query) ? state : !state;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!_proto.TryIndex<BreedPrototype>(RootBreed, out var root))
            return false;

        var tiers = ComputeTiers(root);
        var center = new Vector2(CanvasSize / 2f, CanvasSize / 2f);

        foreach (var breed in _proto.EnumeratePrototypes<BreedPrototype>())
        {
            if (!tiers.TryGetValue(breed.ID, out var tier))
                continue;

            if (breed.ID == RootBreed)
            {
                _nodes.Add(new BreedNode(breed.ID, breed, tier, center, GreySize / 2f));
                continue;
            }

            if (!BreedAngles.TryGetValue(breed.ID, out var degrees))
                continue;

            var radians = MathHelper.DegreesToRadians(degrees);
            var dir = new Vector2(MathF.Cos(radians), MathF.Sin(radians));
            var pos = center + dir * RingRadii[tier];

            _nodes.Add(new BreedNode(breed.ID, breed, tier, pos, NodeSize / 2f));
        }

        foreach (var node in _nodes)
        {
            foreach (var mutation in node.Breed.PotentialMutations)
            {
                if (!tiers.TryGetValue(mutation.Id, out var childTier) || childTier <= node.Tier)
                    continue;

                var child = _nodes.FirstOrDefault(x => x.Id == mutation.Id);
                if (child == null)
                    continue;

                _edges.Add((node, child));
            }
        }

        MinSize = new Vector2(CanvasSize, CanvasSize);

        control = this;
        return true;
    }

    private Dictionary<string, int> ComputeTiers(BreedPrototype root)
    {
        var tiers = new Dictionary<string, int> { [root.ID] = 0 };
        var queue = new Queue<BreedPrototype>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var breed = queue.Dequeue();
            var tier = tiers[breed.ID];

            foreach (var mutation in breed.PotentialMutations)
            {
                if (tiers.ContainsKey(mutation.Id))
                    continue;

                if (!_proto.TryIndex(mutation, out var child))
                    continue;

                tiers[mutation.Id] = tier + 1;
                queue.Enqueue(child);
            }
        }

        return tiers;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        _hoveredNode = GetHoveredNode(args.RelativePosition);
    }

    protected override void MouseExited()
    {
        base.MouseExited();

        _hoveredNode = null;
    }

    private BreedNode? GetHoveredNode(Vector2 relativePosition)
    {
        foreach (var node in _nodes)
        {
            if ((relativePosition - node.Pos).LengthSquared() <= node.Radius * node.Radius)
                return node;
        }

        return null;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        DrawEdges(handle);

        foreach (var node in _nodes)
        {
            var pos = ToScreen(node.Pos);
            handle.DrawCircle(pos + new Vector2(0f, 2f), node.Radius + 2f, Color.Black.WithAlpha(0.35f));
        }

        foreach (var node in _nodes.OrderBy(x => x.Tier))
        {
            var pos = ToScreen(node.Pos);
            DrawSlime(handle, pos, node.Radius * 2f, node.Breed.SlimeColor);
        }

        if (_hoveredNode is { } hovered)
            DrawNameplate(handle, hovered);
    }

    private void DrawNameplate(DrawingHandleScreen handle, BreedNode node)
    {
        var name = Loc.GetString(node.Breed.BreedName);
        var dimensions = handle.GetDimensions(_font, name, 1);
        var padding = new Vector2(6f, 3f);

        var textPos = node.Pos + new Vector2(-dimensions.X / 2f, -node.Radius - dimensions.Y - padding.Y - 6f);
        var box = UIBox2.FromDimensions(textPos - padding, dimensions + padding * 2f);

        handle.DrawRect(box, Color.FromHex("#141318").WithAlpha(0.92f));
        handle.DrawRect(box, node.Breed.SlimeColor, false);
        handle.DrawString(_font, ToScreen(textPos), name, Color.White);
    }

    private void DrawEdges(DrawingHandleScreen handle)
    {
        foreach (var (parent, child) in _edges)
        {
            var dir = (child.Pos - parent.Pos).Normalized();
            var from = ToScreen(parent.Pos + dir * parent.Radius);
            var to = ToScreen(child.Pos - dir * child.Radius);

            var color = parent.Tier == 0
                ? XenobiologyGuideColors.Accent.WithAlpha(0.9f)
                : XenobiologyGuideColors.Arrow;

            handle.DrawLine(from, to, color);
            DrawArrowhead(handle, from, to, color);
        }
    }

    private void DrawSlime(DrawingHandleScreen handle, Vector2 pos, float size, Color color)
    {
        if (_slimeTexture == null)
        {
            handle.DrawCircle(pos, size / 2f, color);
            return;
        }

        var rect = UIBox2.FromDimensions(pos - new Vector2(size / 2f, size / 2f), new Vector2(size, size));
        handle.DrawTextureRect(_slimeTexture, rect, color);
    }

    private void DrawArrowhead(DrawingHandleScreen handle, Vector2 from, Vector2 to, Color color)
    {
        var dir = (to - from).Normalized();
        var tip = to;
        var back = tip - dir * 7f;
        var perp = new Vector2(-dir.Y, dir.X) * 3.5f;

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList,
            new[] { tip, back + perp, back - perp },
            color);
    }

    private Vector2 ToScreen(Vector2 local)
    {
        return local * UIScale;
    }
}