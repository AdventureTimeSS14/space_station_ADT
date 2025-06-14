using System.Collections.Generic;
using System.Linq;

namespace Content.Shared.ADT.CommandConsole;

// Абстрактный базовый класс для файловой системы
[DataDefinition]
public abstract partial class FileNode
{
    [DataField(required: true)]
    public string Name = string.Empty;

    [NonSerialized]
    public Directory? Parent;

    public string GetPath()
    {
        return Parent == null ? "/" : $"{Parent.GetPath().TrimEnd('/')}/{Name}";
    }
}

// Файл
[DataDefinition]
public sealed partial class File : FileNode
{
    [DataField]
    public string Content = string.Empty;
}

// Директория
[DataDefinition]
public sealed partial class Directory : FileNode
{
    [DataField]
    public List<FileNode> Children = new();

    public FileNode? Get(string name) => Children.FirstOrDefault(f => f.Name == name);

    public void Add(FileNode node)
    {
        node.Parent = this;
        Children.Add(node);
    }
}

public static class FileSystem
{
    public static Directory RootDirectory { get; set; } = new() { Name = "" };

    public static FileNode? ResolvePath(string path, Directory currentDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        Directory? current = path.StartsWith("/") ? RootDirectory : currentDirectory;

        foreach (var part in parts.SkipLast(1))
        {
            if (current?.Get(part) is Directory next)
            {
                current = next;
            }
            else
            {
                return null;
            }
        }

        return current?.Get(parts.Last());
    }

    public static File? ResolvePathToFile(string path, Directory currentDirectory)
    {
        return ResolvePath(path, currentDirectory) as File;
    }
}
