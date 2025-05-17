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
