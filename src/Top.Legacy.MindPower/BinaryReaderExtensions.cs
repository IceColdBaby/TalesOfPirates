using System.IO;
using System.Numerics;

namespace Top.Legacy.MindPower
{
    internal static class BinaryReaderExtensions
    {
        internal static float[] ReadMatrix44Raw(this BinaryReader r)
        {
            var a = new float[16];

            for (var i = 0; i < 16; i++)
            {
                a[i] = r.ReadSingle();
            }

            return a;
        }

        internal static Matrix4x4 ReadMatrix43(this BinaryReader r)
        {
            var m = r.ReadFloats(12);
            return new Matrix4x4(
                m[0], m[1], m[2], 0f,
                m[3], m[4], m[5], 0f,
                m[6], m[7], m[8], 0f,
                m[9], m[10], m[11], 1f);
        }

        internal static Vector2 ReadVector2(this BinaryReader r)
        {
            return new Vector2(r.ReadSingle(), r.ReadSingle());
        }

        internal static Vector3 ReadVector3(this BinaryReader r)
        {
            return new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        }

        internal static Vector4 ReadVector4(this BinaryReader r)
        {
            return new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        }

        internal static Quaternion ReadQuaternion(this BinaryReader r)
        {
            return new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        }

        internal static Quaternion[] ReadQuaternionArray(this BinaryReader r, int n)
        {
            var a = new Quaternion[n];

            for (var i = 0; i < n; i++)
            {
                a[i] = r.ReadQuaternion();
            }

            return a;
        }

        internal static Vector2[] ReadVec2Array(this BinaryReader r, int n)
        {
            var a = new Vector2[n];

            for (var i = 0; i < n; i++)
            {
                a[i] = r.ReadVector2();
            }

            return a;
        }

        internal static Vector3[] ReadVec3Array(this BinaryReader r, int n)
        {
            var a = new Vector3[n];

            for (var i = 0; i < n; i++)
            {
                a[i] = r.ReadVector3();
            }

            return a;
        }

        internal static uint[] ReadUintArray(this BinaryReader r, int n)
        {
            var a = new uint[n];

            for (var i = 0; i < n; i++)
            {
                a[i] = r.ReadUInt32();
            }

            return a;
        }

        internal static float[] ReadFloats(this BinaryReader r, int n)
        {
            var a = new float[n];

            for (var i = 0; i < n; i++)
            {
                a[i] = r.ReadSingle();
            }

            return a;
        }

        internal static RgbaF ReadRgbaF(this BinaryReader r)
        {
            return new RgbaF(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        }

        internal static RgbaF[] ReadColors(this BinaryReader r, int n)
        {
            var a = new RgbaF[n];

            for (var i = 0; i < n; i++)
            {
                a[i] = r.ReadRgbaF();
            }

            return a;
        }

        internal static Rgba32 ReadRgba32(this BinaryReader r)
        {
            return Rgba32.FromArgb(r.ReadUInt32());
        }

        internal static string ReadFixedString(this BinaryReader r, int length)
        {
            var bytes = r.ReadBytes(length);
            var nul = System.Array.IndexOf(bytes, (byte)0);
            return Text.Gbk.GetString(bytes, 0, nul >= 0 ? nul : bytes.Length);
        }
    }
}
