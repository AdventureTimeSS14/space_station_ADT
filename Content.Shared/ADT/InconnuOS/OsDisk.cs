using Content.Shared.ADT.LogicCircuit;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.InconnuOS;

[Serializable, NetSerializable]
public enum OsFileKind : byte
{
    Directory = 0,
    Text,
    Circuit,
    Log,
    Shortcut,
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class OsFile
{
    public const int CircuitNodeWeight = 32;

    public const int CircuitWireWeight = 8;

    [DataField(required: true)]
    public string Path = string.Empty;

    [DataField]
    public OsFileKind Kind = OsFileKind.Text;

    [DataField]
    public string Text = string.Empty;

    [DataField]
    public LogicCircuitLayout? Circuit;

    [DataField]
    public bool ReadOnly;

    [DataField]
    public bool Hidden;

    [DataField]
    public long DisplaySize;

    [DataField]
    public bool Critical;

    [DataField]
    public TimeSpan Modified;

    public string Name => OsPath.GetName(Path);

    public string Extension => OsPath.GetExtension(Path);

    public bool IsDirectory => Kind == OsFileKind.Directory;

    public long ShownSize => DisplaySize > 0 ? DisplaySize : Size;

    public int Size
    {
        get
        {
            var size = Text.Length;

            if (Circuit != null)
                size += Circuit.Nodes.Count * CircuitNodeWeight + Circuit.Wires.Count * CircuitWireWeight;

            return size;
        }
    }

    public OsFile Clone()
    {
        return new OsFile
        {
            Path = Path,
            Kind = Kind,
            Text = Text,
            Circuit = Circuit?.Clone(),
            ReadOnly = ReadOnly,
            Hidden = Hidden,
            DisplaySize = DisplaySize,
            Critical = Critical,
            Modified = Modified,
        };
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class OsDisk
{
    [DataField]
    public List<OsFile> Files = new();

    public int Count => Files.Count;

    public int TotalSize
    {
        get
        {
            var total = 0;

            foreach (var file in Files)
            {
                total += file.Size;
            }

            return total;
        }
    }

    public int IndexOf(string path)
    {
        for (var i = 0; i < Files.Count; i++)
        {
            if (Files[i].Path.Equals(path, OsPath.Comparison))
                return i;
        }

        return -1;
    }

    public OsFile? Find(string path)
    {
        var index = IndexOf(path);

        if (index < 0)
            return null;

        return Files[index];
    }

    public bool Contains(string path)
    {
        return IndexOf(path) >= 0;
    }

    public bool DirectoryExists(string path)
    {
        if (OsPath.IsRoot(path))
            return true;

        foreach (var file in Files)
        {
            if (file.IsDirectory && file.Path.Equals(path, OsPath.Comparison))
                return true;

            if (OsPath.IsInside(file.Path, path))
                return true;
        }

        return false;
    }

    public List<OsFile> List(string directory)
    {
        var result = new List<OsFile>();
        var implicitDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Files)
        {
            if (OsPath.IsDirectlyInside(file.Path, directory))
            {
                result.Add(file);
                continue;
            }

            if (!OsPath.IsInside(file.Path, directory))
                continue;

            var parent = OsPath.GetParent(file.Path);
            while (!OsPath.IsDirectlyInside(parent, directory) && !OsPath.IsRoot(parent))
            {
                parent = OsPath.GetParent(parent);
            }

            if (!implicitDirs.Add(parent))
                continue;

            result.Add(new OsFile
            {
                Path = parent,
                Kind = OsFileKind.Directory,
            });
        }

        for (var i = result.Count - 1; i >= 0; i--)
        {
            if (!result[i].IsDirectory)
                continue;

            for (var j = 0; j < i; j++)
            {
                if (result[j].IsDirectory && result[j].Path.Equals(result[i].Path, OsPath.Comparison))
                {
                    result.RemoveAt(i);
                    break;
                }
            }
        }

        result.Sort(Compare);
        return result;
    }

    private static int Compare(OsFile a, OsFile b)
    {
        if (a.IsDirectory != b.IsDirectory)
            return a.IsDirectory ? -1 : 1;

        return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
    }

    public OsDisk Clone()
    {
        var result = new OsDisk
        {
            Files = new List<OsFile>(Files.Count),
        };

        foreach (var file in Files)
        {
            result.Files.Add(file.Clone());
        }

        return result;
    }
}
