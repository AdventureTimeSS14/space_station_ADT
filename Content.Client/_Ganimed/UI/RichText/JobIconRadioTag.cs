using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._Ganimed.UI.RichText
{
    public sealed class RadioIconTag : IMarkupTag
    {
        [Dependency] private readonly IResourceCache _cache = default!;

        public string Name => "radicon";

        public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
        {
            control = null;

            if (!node.Attributes.TryGetValue("path", out var rawPath))
            {
                return false;
            }

            if (!node.Attributes.TryGetValue("scale", out var scaleStr) || !scaleStr.TryGetLong(out var scaleValue))
            {
                scaleValue = 1;
            }

            control = DrawIcon(rawPath.ToString(), scaleValue.Value);
            return true;
        }

        private Control DrawIcon(string path, long scale)
        {
            var textureRect = new TextureRect();

            path = ClearString(path);

            textureRect.TexturePath = path;
            textureRect.TextureScale = new Vector2(scale, scale);

            return textureRect;
        }

        private static string ClearString(string str)
        {
            return str.Replace("=", "")
                      .Replace(" ", "")
                      .Replace("\"", "");
        }
    }
}
