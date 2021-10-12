using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;

namespace Helion.Render.Legacy.Buffer.Array;

public abstract class ArrayBufferObject<T> : BufferObject<T> where T : struct
{
    protected ArrayBufferObject(GLCapabilities capabilities, IGLFunctions functions, string objectLabel = "") :
        base(capabilities, functions, objectLabel)
    {
    }

    protected override BufferType GetBufferType() => BufferType.ArrayBuffer;

    protected override void PerformUpload()
    {
        gl.BufferData(GetBufferType(), BytesPerElement * Data.Length, Data.Data, GetBufferUsageType());
    }

    protected abstract BufferUsageType GetBufferUsageType();
}

