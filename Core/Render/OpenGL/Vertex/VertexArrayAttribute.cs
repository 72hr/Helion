using Helion.Render.OpenGL.Context;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Vertex;

public abstract class VertexArrayAttribute
{
    public readonly string Name;
    public readonly int Index;
    public readonly int Size;

    public abstract int ByteLength { get; }

    protected VertexArrayAttribute(string name, int index, int size)
    {
        Precondition(name.Length > 0, "Cannot have an empty VAO attribute name");
        Precondition(index >= 0, "VAO attribute index must be positive");
        Precondition(size > 0, "Cannot have a VAO attribute with no size");

        Name = name;
        Index = index;
        Size = size;
    }

    public abstract void Enable(int stride, int offset);
}