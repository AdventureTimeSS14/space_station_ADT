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

    private const float RowSpacing = 90f;
    private const float RowGap = 96f;
    private const float Row1X = 130f;
    private const float Row2X = 178f;
    private const float RowStartY = 70f;
    private const float NodeSize = 34f;
    private const float GreySize = 52f;
    private const float LabelHeight = 13f;

    private const float CanvasWidth = Row2X + RowGap * 3 + NodeSize + 14f;
    private const float CanvasHeight = RowStartY + RowSpacing * 5 + NodeSize + LabelHeight + 14f;
    private static readonly Dictionary<string, (int Row, int Slot)> BreedSlots = new()
    {
        [RootBreed] = (0, 0),
        ["OrangeMutation"] = (1, 0),
        ["PurpleMutation"] = (1, 1),
        ["BlueMutation"] = (1, 2),
        ["MetalMutation"] = (1, 3),
        ["YellowMutation"] = (2, 0),
        ["DarkPurpleMutation"] = (2, 1),
        ["DarkBlueMutation"] = (2, 2),
        ["SilverMutation"] = (2, 3),
        ["RedMutation"] = (3, 0),
        ["GreenMutation"] = (3, 1),
        ["PinkMutation"] = (3, 2),
        ["GoldMutation"] = (3, 3),
        ["BluespaceMutation"] = (4, 0),
        ["SepiaMutation"] = (4, 1),
        ["CeruleanMutation"] = (4, 2),
        ["PyriteMutation"] = (4, 3),
        ["OilMutation"] = (5, 0),
        ["BlackMutation"] = (5, 1),
        ["LightPinkMutation"] = (5, 2),
        ["AdamantineMutation"] = (5, 3),
    };

    private readonly IPrototypeManager _proto;
    private readonly Font _font;
    private readonly Texture _slimeTexture;

    private readonly List<BreedNode> _nodes = new();
    private readonly List<(BreedNode Parent, BreedNode Child)> _edges = new();

    private sealed record BreedNode(ProtoId<BreedPrototype> Id, BreedPrototype Breed, int Tier, Vector2 Pos, float Radius);

    public GuideXenobiologyTreeEmbed()
    {
        IoCManager.InjectDependencies(this);
        _proto = IoCManager.Resolve<IPrototypeManager>();

        var resource = IoCManager.Resolve<IResourceCache>();
        _font = resource.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 10);

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
        var centerX = CanvasWidth / 2f;

        foreach (var breed in _proto.EnumeratePrototypes<BreedPrototype>())
        {
            if (!tiers.TryGetValue(breed.ID, out var tier))
                continue;

            if (!BreedSlots.TryGetValue(breed.ID, out var slot))
                continue;

            var isRoot = breed.ID == RootBreed;
            var radius = isRoot ? GreySize / 2f : NodeSize / 2f;
            var x = isRoot ? centerX : (slot.Row % 2 == 0 ? Row1X : Row2X) + slot.Slot * RowGap;
            var pos = new Vector2(x, RowY(slot.Row));

            _nodes.Add(new BreedNode(breed.ID, breed, tier, pos, radius));
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

        MinSize = new Vector2(CanvasWidth, CanvasHeight);

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

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        DrawEdges(handle);

        foreach (var node in _nodes)
        {
            var pos = ToScreen(node.Pos);
            handle.DrawCircle(pos + new Vector2(0f, 2f), node.Radius + 2f, Color.Black.WithAlpha(0.35f));
        }

        foreach (var node in _nodes.OrderBy(x => x.Pos.Y))
        {
            var pos = ToScreen(node.Pos);

            if (node.Breed.ID == RootBreed)
                handle.DrawCircle(pos, node.Radius + 5f, XenobiologyGuideColors.Accent.WithAlpha(0.4f), false);

            handle.DrawCircle(pos, node.Radius + 3f, node.Breed.SlimeColor.WithAlpha(0.25f));

            DrawSlime(handle, pos, node.Radius * 2f, node.Breed.SlimeColor);

            var name = Loc.GetString(node.Breed.BreedName).Replace(" слайм", "");
            var dimensions = handle.GetDimensions(_font, name, 1);
            var labelPos = ToScreen(node.Pos + new Vector2(-dimensions.X / 2f, node.Radius + 4f));
            handle.DrawString(_font, labelPos, name, XenobiologyGuideColors.Text);
        }
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

    private static float RowY(int row)
    {
        return RowStartY + row * RowSpacing;
    }

    private Vector2 ToScreen(Vector2 local)
    {
        return local * UIScale;
    }
}