using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using Content.Shared.ADT.LogicCircuit;

namespace Content.Client.ADT.LogicCircuit;

public static class LogicCircuitJson
{
    public const int MaxFileSize = 256 * 1024;
    public const string Format = "adt-logic-circuit";
    public const int Version = 1;

    private const int MaxDepth = 6;
    private const int MaxValues = 50000;
    private const int MaxStringLength = 4096;
    private const int MaxNumberLength = 64;
    private const int MaxItems = 1024;
    private const int MaxConfigItems = 64;
    private const int MaxInteger = 1000000;
    private const int MaxDetailLength = 48;

    private static readonly string[] RootKeys = { "format", "version", "nodes", "wires" };
    private static readonly string[] NodeKeys = { "id", "proto", "x", "y", "config" };
    private static readonly string[] WireKeys = { "from", "fromPin", "to", "toPin", "color" };

    private sealed class ReadError : Exception
    {
        public readonly string Key;
        public readonly string Detail;

        public ReadError(string key, string detail)
        {
            Key = key;
            Detail = detail.Length > MaxDetailLength ? detail.Substring(0, MaxDetailLength) : detail;
        }
    }

    public static string Write(LogicCircuitLayout layout)
    {
        var builder = new StringBuilder();

        builder.Append("{\n  \"format\": ");
        AppendString(builder, Format);
        builder.Append(",\n  \"version\": ");
        builder.Append(Version.ToString(CultureInfo.InvariantCulture));
        builder.Append(",\n  \"nodes\": [");

        for (var i = 0; i < layout.Nodes.Count; i++)
        {
            var node = layout.Nodes[i];

            builder.Append(i == 0 ? "\n    {\"id\": " : ",\n    {\"id\": ");
            AppendString(builder, node.Id);
            builder.Append(", \"proto\": ");
            AppendString(builder, node.Proto.Id);
            builder.Append(", \"x\": ");
            AppendNumber(builder, node.Position.X);
            builder.Append(", \"y\": ");
            AppendNumber(builder, node.Position.Y);
            builder.Append(", \"config\": [");

            for (var j = 0; j < node.Config.Count; j++)
            {
                if (j > 0)
                    builder.Append(", ");

                AppendString(builder, node.Config[j].AsText());
            }

            builder.Append("]}");
        }

        builder.Append(layout.Nodes.Count == 0 ? "],\n  \"wires\": [" : "\n  ],\n  \"wires\": [");

        for (var i = 0; i < layout.Wires.Count; i++)
        {
            var wire = layout.Wires[i];

            builder.Append(i == 0 ? "\n    {\"from\": " : ",\n    {\"from\": ");
            AppendString(builder, wire.From);
            builder.Append(", \"fromPin\": ");
            builder.Append(wire.FromPin.ToString(CultureInfo.InvariantCulture));
            builder.Append(", \"to\": ");
            AppendString(builder, wire.To);
            builder.Append(", \"toPin\": ");
            builder.Append(wire.ToPin.ToString(CultureInfo.InvariantCulture));
            builder.Append(", \"color\": ");
            builder.Append(wire.Color.ToString(CultureInfo.InvariantCulture));
            builder.Append('}');
        }

        builder.Append(layout.Wires.Count == 0 ? "]\n}\n" : "\n  ]\n}\n");

        return builder.ToString();
    }

    public static string? ReadLimited(Stream stream)
    {
        if (stream.CanSeek && stream.Length > MaxFileSize)
            return null;

        using var reader = new StreamReader(stream, Encoding.UTF8, true);

        var buffer = new char[MaxFileSize + 1];
        var total = 0;

        while (total < buffer.Length)
        {
            var read = reader.ReadBlock(buffer, total, buffer.Length - total);
            if (read == 0)
                break;

            total += read;
        }

        if (total > MaxFileSize)
            return null;

        return new string(buffer, 0, total);
    }

    public static bool TryRead(string text, [NotNullWhen(true)] out LogicCircuitLayout? layout, out string error)
    {
        layout = null;
        error = string.Empty;

        try
        {
            var root = new Parser(text).ParseDocument();
            layout = Map(root);
            return true;
        }
        catch (ReadError e)
        {
            error = Loc.GetString(e.Key, ("detail", e.Detail));
            return false;
        }
        catch (Exception)
        {
            error = Loc.GetString("logic-circuit-json-error-read");
            return false;
        }
    }

    private static LogicCircuitLayout Map(object? root)
    {
        var obj = AsObject(root, "root");
        CheckKeys(obj, "root", RootKeys);

        if (GetString(obj, "format", "root") != Format)
            throw new ReadError("logic-circuit-json-error-format", "format");

        var version = GetInt(obj, "version", "root");
        if (version != Version)
            throw new ReadError("logic-circuit-json-error-version", version.ToString(CultureInfo.InvariantCulture));

        var nodes = GetArray(obj, "nodes", "root");
        var wires = GetArray(obj, "wires", "root");

        if (nodes.Count > MaxItems)
            throw new ReadError("logic-circuit-json-error-format", "nodes");

        if (wires.Count > MaxItems)
            throw new ReadError("logic-circuit-json-error-format", "wires");

        var layout = new LogicCircuitLayout
        {
            Nodes = new List<LogicNodeData>(nodes.Count),
            Wires = new List<LogicWireData>(wires.Count),
        };

        for (var i = 0; i < nodes.Count; i++)
        {
            var path = $"nodes[{i}]";
            var node = AsObject(nodes[i], path);
            CheckKeys(node, path, NodeKeys);

            var data = new LogicNodeData
            {
                Id = GetString(node, "id", path),
                Proto = GetString(node, "proto", path),
                Position = new Vector2(GetFloat(node, "x", path), GetFloat(node, "y", path)),
            };

            if (node.TryGetValue("config", out var configValue))
            {
                var configPath = path + ".config";
                var config = AsArray(configValue, configPath);

                if (config.Count > MaxConfigItems)
                    throw new ReadError("logic-circuit-json-error-format", configPath);

                foreach (var item in config)
                {
                    data.Config.Add(LogicSignal.FromText(AsString(item, configPath), 0));
                }
            }

            layout.Nodes.Add(data);
        }

        for (var i = 0; i < wires.Count; i++)
        {
            var path = $"wires[{i}]";
            var wire = AsObject(wires[i], path);
            CheckKeys(wire, path, WireKeys);

            layout.Wires.Add(new LogicWireData
            {
                From = GetString(wire, "from", path),
                FromPin = GetInt(wire, "fromPin", path),
                To = GetString(wire, "to", path),
                ToPin = GetInt(wire, "toPin", path),
                Color = wire.ContainsKey("color") ? GetInt(wire, "color", path) : 0,
            });
        }

        return layout;
    }

    private static void CheckKeys(Dictionary<string, object?> obj, string path, string[] allowed)
    {
        foreach (var key in obj.Keys)
        {
            if (Array.IndexOf(allowed, key) < 0)
                throw new ReadError("logic-circuit-json-error-format", path + "." + key);
        }
    }

    private static object? Get(Dictionary<string, object?> obj, string key, string path)
    {
        if (!obj.TryGetValue(key, out var value))
            throw new ReadError("logic-circuit-json-error-format", path + "." + key);

        return value;
    }

    private static string GetString(Dictionary<string, object?> obj, string key, string path)
    {
        return AsString(Get(obj, key, path), path + "." + key);
    }

    private static List<object?> GetArray(Dictionary<string, object?> obj, string key, string path)
    {
        return AsArray(Get(obj, key, path), path + "." + key);
    }

    private static float GetFloat(Dictionary<string, object?> obj, string key, string path)
    {
        if (Get(obj, key, path) is not double value)
            throw new ReadError("logic-circuit-json-error-format", path + "." + key);

        return (float) value;
    }

    private static int GetInt(Dictionary<string, object?> obj, string key, string path)
    {
        if (Get(obj, key, path) is not double value
            || Math.Floor(value) != value
            || Math.Abs(value) > MaxInteger)
        {
            throw new ReadError("logic-circuit-json-error-format", path + "." + key);
        }

        return (int) value;
    }

    private static Dictionary<string, object?> AsObject(object? value, string path)
    {
        if (value is not Dictionary<string, object?> obj)
            throw new ReadError("logic-circuit-json-error-format", path);

        return obj;
    }

    private static List<object?> AsArray(object? value, string path)
    {
        if (value is not List<object?> list)
            throw new ReadError("logic-circuit-json-error-format", path);

        return list;
    }

    private static string AsString(object? value, string path)
    {
        if (value is not string text)
            throw new ReadError("logic-circuit-json-error-format", path);

        return text;
    }

    private static void AppendString(StringBuilder builder, string value)
    {
        builder.Append('"');

        foreach (var symbol in value)
        {
            switch (symbol)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                default:
                    if (char.IsControl(symbol) || char.IsSurrogate(symbol))
                    {
                        builder.Append("\\u");
                        builder.Append(((int) symbol).ToString("x4", CultureInfo.InvariantCulture));
                        break;
                    }

                    builder.Append(symbol);
                    break;
            }
        }

        builder.Append('"');
    }

    private static void AppendNumber(StringBuilder builder, float value)
    {
        if (!float.IsFinite(value))
            value = 0f;

        builder.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
    }

    private sealed class Parser
    {
        private readonly string _text;
        private int _pos;
        private int _values;

        public Parser(string text)
        {
            _text = text;
        }

        public object? ParseDocument()
        {
            var value = ParseValue(0);

            SkipWhitespace();

            if (_pos != _text.Length)
                throw Syntax();

            return value;
        }

        private object? ParseValue(int depth)
        {
            if (depth >= MaxDepth || ++_values > MaxValues)
                throw Syntax();

            SkipWhitespace();

            var symbol = Peek();
            switch (symbol)
            {
                case '{':
                    return ParseObject(depth);
                case '[':
                    return ParseArray(depth);
                case '"':
                    return ParseString();
                case 't':
                    ExpectWord("true");
                    return true;
                case 'f':
                    ExpectWord("false");
                    return false;
                case 'n':
                    ExpectWord("null");
                    return null;
            }

            if (symbol == '-' || symbol is >= '0' and <= '9')
                return ParseNumber();

            throw Syntax();
        }

        private Dictionary<string, object?> ParseObject(int depth)
        {
            _pos++;

            var result = new Dictionary<string, object?>();

            SkipWhitespace();

            if (Peek() == '}')
            {
                _pos++;
                return result;
            }

            while (true)
            {
                SkipWhitespace();

                if (Peek() != '"')
                    throw Syntax();

                var key = ParseString();

                SkipWhitespace();
                Expect(':');

                var value = ParseValue(depth + 1);

                if (!result.TryAdd(key, value))
                    throw Syntax();

                SkipWhitespace();

                if (Peek() == ',')
                {
                    _pos++;
                    continue;
                }

                Expect('}');
                return result;
            }
        }

        private List<object?> ParseArray(int depth)
        {
            _pos++;

            var result = new List<object?>();

            SkipWhitespace();

            if (Peek() == ']')
            {
                _pos++;
                return result;
            }

            while (true)
            {
                result.Add(ParseValue(depth + 1));

                SkipWhitespace();

                if (Peek() == ',')
                {
                    _pos++;
                    continue;
                }

                Expect(']');
                return result;
            }
        }

        private string ParseString()
        {
            _pos++;

            var builder = new StringBuilder();

            while (true)
            {
                if (_pos >= _text.Length)
                    throw Syntax();

                var symbol = _text[_pos++];

                if (symbol == '"')
                    break;

                if (symbol == '\\')
                {
                    if (_pos >= _text.Length)
                        throw Syntax();

                    var escape = _text[_pos++];
                    switch (escape)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            builder.Append(escape);
                            break;
                        case 'u':
                            builder.Append(ParseHex());
                            break;
                        default:
                            throw Syntax();
                    }
                }
                else
                {
                    builder.Append(symbol);
                }

                if (builder.Length > MaxStringLength)
                    throw Syntax();
            }

            var result = builder.ToString();

            for (var i = 0; i < result.Length; i++)
            {
                var symbol = result[i];

                if (char.IsControl(symbol) || char.IsLowSurrogate(symbol))
                    throw Syntax();

                if (!char.IsHighSurrogate(symbol))
                    continue;

                if (i + 1 >= result.Length || !char.IsLowSurrogate(result[i + 1]))
                    throw Syntax();

                i++;
            }

            return result;
        }

        private char ParseHex()
        {
            if (_pos + 4 > _text.Length)
                throw Syntax();

            if (!int.TryParse(_text.Substring(_pos, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code))
                throw Syntax();

            _pos += 4;
            return (char) code;
        }

        private double ParseNumber()
        {
            var start = _pos;

            while (_pos < _text.Length && IsNumberSymbol(_text[_pos]))
            {
                _pos++;
            }

            var length = _pos - start;
            if (length == 0 || length > MaxNumberLength)
                throw Syntax();

            const NumberStyles styles = NumberStyles.AllowLeadingSign
                                        | NumberStyles.AllowDecimalPoint
                                        | NumberStyles.AllowExponent;

            if (!double.TryParse(_text.Substring(start, length), styles, CultureInfo.InvariantCulture, out var number)
                || !double.IsFinite(number))
            {
                throw Syntax();
            }

            return number;
        }

        private static bool IsNumberSymbol(char symbol)
        {
            return symbol is >= '0' and <= '9' or '-' or '+' or '.' or 'e' or 'E';
        }

        private void ExpectWord(string word)
        {
            if (_pos + word.Length > _text.Length || string.CompareOrdinal(_text, _pos, word, 0, word.Length) != 0)
                throw Syntax();

            _pos += word.Length;
        }

        private void Expect(char symbol)
        {
            if (Peek() != symbol)
                throw Syntax();

            _pos++;
        }

        private char Peek()
        {
            return _pos < _text.Length ? _text[_pos] : '\0';
        }

        private void SkipWhitespace()
        {
            while (_pos < _text.Length && _text[_pos] is ' ' or '\t' or '\n' or '\r')
            {
                _pos++;
            }
        }

        private ReadError Syntax()
        {
            return new ReadError("logic-circuit-json-error-syntax", _pos.ToString(CultureInfo.InvariantCulture));
        }
    }
}
