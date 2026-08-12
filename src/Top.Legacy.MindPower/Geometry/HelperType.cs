using System;

namespace Top.Legacy.MindPower.Geometry
{
    /// <summary>
    /// Bit-mask of present helper sub-blocks.
    /// <br/> lwHelperInfo type field (lwExpObj.h)
    /// </summary>
    [Flags]
    public enum HelperType : uint
    {
        Dummy = 1,
        Box = 2,
        Mesh = 4,
        BoundingBox = 16,
        BoundingSphere = 32,
    }
}
