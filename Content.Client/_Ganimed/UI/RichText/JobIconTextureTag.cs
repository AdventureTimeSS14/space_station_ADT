using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._Ganimed.UI.RichText
{
    public sealed class TextureTag : IMarkupTag
    {
        public string Name => "tex";

        public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
        {
            control = null;

            if (!node.Attributes.TryGetValue("path", out var rawPath))
            {
                return false;
            }

            if (!node.Attributes.TryGetValue("scale", out var scale) || !scale.TryGetLong(out var scaleValue))
            {
                scaleValue = 1;
            }

            var textureRect = new TextureRect();

            var path = SanitizeString(rawPath.ToString());

            textureRect.TexturePath = path;
            textureRect.TextureScale = new Vector2(scaleValue.Value, scaleValue.Value);

            control = textureRect;
            return true;
        }

        private static string SanitizeString(string input)
        {
            return input.Replace("=", "")
                        .Replace(" ", "")
                        .Replace("\"", "");
        }
    }
}