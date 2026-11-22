#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.ADT;

[TestFixture]
public sealed class ShaderCompatibilityTest : GameTest
{
    [SidedDependency(Side.Client)]
    private readonly IResourceManager _resources = null!;

    private static readonly ResPath ShaderRoot = new("/Textures/");

    private static readonly string[] OwnedRoots =
    {
        "/Textures/ADT/",
    };

    private static readonly Regex DirectiveRegex =
        new(@"^[ \t]*#[ \t]*(?<kind>ifdef|ifndef|else|endif)\b[ \t]*(?<name>[A-Za-z0-9_]*)", RegexOptions.Compiled);

    private sealed record CompatRule(string Id, Regex Pattern, string Problem, string Fix, string? GuardDefine = null);

    private static readonly CompatRule[] Rules =
    {
        new("texture-call",
            new Regex(@"(?<![A-Za-z0-9_])texture\s*\(", RegexOptions.Compiled),
            "в GLSL ES 1.00 нет функции texture(): движок подменяет texture2D на texture только вне режима совместимости",
            "zTexture(UV) для основной текстуры, zTextureSpec(sampler, uv) для остальных"),

        new("es3-texture-call",
            new Regex(@"(?<![A-Za-z0-9_])(?:texelFetch|textureSize|textureLod|textureGrad|textureProj|textureOffset)\s*\(",
                RegexOptions.Compiled),
            "эти выборки текстур появились только в GLSL ES 3.00",
            "нужно считать координаты вручную через TEXTURE_PIXEL_SIZE и брать zTextureSpec"),

        new("uniform-initializer",
            new Regex(@"^[ \t]*uniform\b[^;]*=", RegexOptions.Compiled | RegexOptions.Multiline),
            "GLSL ES запрещает инициализаторы у uniform, и в 1.00, и в 3.00",
            "нужно задать значение через params в прототипе шейдера, как это сделано у ADTThermalBodyShader"),

        new("switch",
            new Regex(@"(?<![A-Za-z0-9_])switch\s*\(", RegexOptions.Compiled),
            "в GLSL ES 1.00 нет switch",
            "нужно разложить на if/else"),

        new("while-loop",
            new Regex(@"(?<![A-Za-z0-9_])while\s*\(", RegexOptions.Compiled),
            "GLSL ES 1.00 гарантирует только простые for-циклы с константной границей",
            "нужно использовать for с константным числом итераций"),

        new("do-loop",
            new Regex(@"(?<![A-Za-z0-9_])do\s*\{", RegexOptions.Compiled),
            "GLSL ES 1.00 гарантирует только простые for-циклы с константной границей",
            "нужно использовать for с константным числом итераций"),

        new("bitwise",
            new Regex(@"<<|>>|(?<![&|])&(?!&)|(?<![&|])\|(?!\|)|(?<!\^)\^(?!\^)", RegexOptions.Compiled),
            "в GLSL ES 1.00 нет побитовых операций над целыми",
            "нужно считать через float-арифметику (floor/mod); логические && || ^^ использовать можно"),

        new("modulo",
            new Regex(@"%", RegexOptions.Compiled),
            "оператора % в GLSL ES 1.00 нет",
            "нужно использовать mod(x, y)"),

        new("es3-builtin",
            new Regex(@"(?<![A-Za-z0-9_])(?:roundEven|round|trunc|isnan|isinf|inverse|transpose|determinant|outerProduct|modf|sinh|cosh|tanh|asinh|acosh|atanh|packHalf2x16|unpackHalf2x16|floatBitsToInt|floatBitsToUint|intBitsToFloat)\s*\(",
                RegexOptions.Compiled),
            "эта встроенная функция появилась только в GLSL ES 3.00",
            "round(x) -> floor(x + 0.5), trunc(x) -> sign(x) * floor(abs(x)), остальное нужно считать руками"),

        new("es3-type",
            new Regex(@"(?<![A-Za-z0-9_])(?:uint|uvec2|uvec3|uvec4)(?![A-Za-z0-9_])|layout\s*\(|(?<![A-Za-z0-9_])flat(?![A-Za-z0-9_])",
                RegexOptions.Compiled),
            "беззнаковых типов, layout-квалификаторов и flat-интерполяции в GLSL ES 1.00 нет",
            "нужно обходится float/int и обычными varying"),

        new("missing-precision",
            new Regex(@"(?<![A-Za-z0-9_])(?<!(?:lowp|mediump|highp)\s+)(?:float|vec2|vec3|vec4|mat2|mat3|mat4)[ \t]+[A-Za-z_]",
                RegexOptions.Compiled),
            "в GLSL ES у float-типов нет точности по умолчанию, а precision-строку никто не подставляет",
            "нужно приписать квалификатор highp для координат и времени, lowp для цветов"),

        new("derivatives",
            new Regex(@"(?<![A-Za-z0-9_])(?:dFdx|dFdy|fwidth)\s*\(", RegexOptions.Compiled),
            "производные в GLES2 доступны только через расширение GL_OES_standard_derivatives, которого может не быть",
            "нужно обернуть в #ifdef HAS_DFDX с запасным значением в #else, как в /Textures/Shaders/cooldown.swsl",
            GuardDefine: "HAS_DFDX"),
    };

    [Test]
    [RunOnSide(Side.Client)]
    public void AllShaderPrototypesLoad()
    {
        var broken = new List<string>();
        var count = 0;

        foreach (var proto in CProtoMan.EnumeratePrototypes<ShaderPrototype>())
        {
            count++;

            try
            {
                proto.InstanceUnique();
            }
            catch (Exception e)
            {
                broken.Add($"{proto.ID}: {e.Message}");
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.GreaterThan(0), "Не нашлось ни одного прототипа шейдера.");
            Assert.That(broken, Is.Empty, $"Прототипы шейдеров не инстанцируются:\n{string.Join('\n', broken)}");
        });
    }

    [Test]
    public void ShadersSurviveCompatMode()
    {
        var files = _resources.ContentFindFiles(ShaderRoot)
            .Where(path => path.Extension == "swsl")
            .OrderBy(path => path.ToString())
            .ToArray();

        var ours = new List<string>();
        var upstream = new List<string>();

        foreach (var file in files)
        {
            var path = file.ToString();
            var isOurs = OwnedRoots.Any(root => path.StartsWith(root, StringComparison.Ordinal));
            var code = StripComments(ReadShader(file));

            foreach (var report in FindViolations(path, code))
            {
                if (isOurs)
                    ours.Add(report);
                else
                    upstream.Add(report);
            }
        }

        foreach (var report in upstream)
        {
            TestContext.Out.WriteLine($"[upstream] {report}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(files, Is.Not.Empty, $"В {ShaderRoot} не нашлось ни одного .swsl.");
            Assert.That(ours, Is.Empty, $"Шейдеры не переживут режим совместимости:\n{string.Join('\n', ours)}");
        });
    }

    private static List<string> FindViolations(string path, string code)
    {
        var reports = new List<string>();
        var lineStarts = LineStarts(code);

        foreach (var rule in Rules)
        {
            var guarded = rule.GuardDefine == null
                ? null
                : GuardedLines(code, rule.GuardDefine);

            foreach (Match match in rule.Pattern.Matches(code))
            {
                var line = LineOf(lineStarts, match.Index);

                if (guarded != null && guarded.Contains(line))
                    continue;

                var found = match.Value.Trim();

                reports.Add($"{path}:{line} [{rule.Id}] '{found}': {rule.Problem}. Как чинить: {rule.Fix}");
            }
        }

        return reports;
    }

    private static HashSet<int> GuardedLines(string code, string define)
    {
        var guarded = new HashSet<int>();

        var stack = new List<(bool Watches, bool Defined)>();
        var lines = code.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var directive = DirectiveRegex.Match(lines[i]);

            if (directive.Success)
            {
                var kind = directive.Groups["kind"].Value;
                var name = directive.Groups["name"].Value;

                switch (kind)
                {
                    case "ifdef":
                        stack.Add((name == define, true));
                        break;

                    case "ifndef":
                        stack.Add((name == define, false));
                        break;

                    case "else":
                        if (stack.Count > 0)
                            stack[^1] = (stack[^1].Watches, !stack[^1].Defined);
                        break;

                    case "endif":
                        if (stack.Count > 0)
                            stack.RemoveAt(stack.Count - 1);
                        break;
                }

                continue;
            }

            if (stack.Any(frame => frame.Watches && frame.Defined))
                guarded.Add(i + 1);
        }

        return guarded;
    }

    private string ReadShader(ResPath path)
    {
        using var stream = _resources.ContentFileRead(path);
        using var reader = new StreamReader(stream, EncodingHelpers.UTF8);

        return reader.ReadToEnd();
    }

    private static string StripComments(string source)
    {
        var builder = new StringBuilder(source.Length);
        var inLine = false;
        var inBlock = false;

        for (var i = 0; i < source.Length; i++)
        {
            var current = source[i];
            var next = i + 1 < source.Length ? source[i + 1] : '\0';

            if (inLine)
            {
                if (current == '\n')
                {
                    inLine = false;
                    builder.Append(current);
                }
                else
                {
                    builder.Append(' ');
                }

                continue;
            }

            if (inBlock)
            {
                if (current == '*' && next == '/')
                {
                    inBlock = false;
                    builder.Append("  ");
                    i++;
                }
                else
                {
                    builder.Append(current == '\n' ? '\n' : ' ');
                }

                continue;
            }

            if (current == '/' && next == '/')
            {
                inLine = true;
                builder.Append("  ");
                i++;
                continue;
            }

            if (current == '/' && next == '*')
            {
                inBlock = true;
                builder.Append("  ");
                i++;
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static List<int> LineStarts(string text)
    {
        var starts = new List<int> { 0 };

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
                starts.Add(i + 1);
        }

        return starts;
    }

    private static int LineOf(List<int> lineStarts, int index)
    {
        var found = lineStarts.BinarySearch(index);

        if (found < 0)
            found = ~found - 1;

        return found + 1;
    }
}
