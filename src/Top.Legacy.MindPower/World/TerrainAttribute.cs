using System;

namespace Top.Legacy.MindPower.World
{
    /// <summary>
    /// The 16 per-tile attribute bits.
    /// <br/> STILE_ATTRIB.attrib (common/include/TerrainAttrib.h)
    /// </summary>
    [Flags]
    public enum TerrainAttribute : ushort
    {
        None = 0,
        Bit1 = 1 << 0,
        Bit2 = 1 << 1,
        Bit3 = 1 << 2,
        Bit4 = 1 << 3,
        Bit5 = 1 << 4,
        Bit6 = 1 << 5,
        Bit7 = 1 << 6,
        Bit8 = 1 << 7,
        Bit9 = 1 << 8,
        Bit10 = 1 << 9,
        Bit11 = 1 << 10,
        Bit12 = 1 << 11,
        Bit13 = 1 << 12,
        Bit14 = 1 << 13,
        Bit15 = 1 << 14,
        Bit16 = 1 << 15,
    }
}
