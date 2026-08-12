using System.IO;
using System.Numerics;

namespace Top.Legacy.MindPower
{
    internal static class BinaryWriterExtensions
    {
        internal static void WriteMatrix44Raw(this BinaryWriter w, float[] a)
        {
            for (var i = 0; i < 16; i++)
            {
                w.Write(a[i]);
            }
        }

        internal static void WriteMatrix44(this BinaryWriter w, Matrix4x4 m)
        {
            w.Write(m.M11);
            w.Write(m.M12);
            w.Write(m.M13);
            w.Write(m.M14);
            w.Write(m.M21);
            w.Write(m.M22);
            w.Write(m.M23);
            w.Write(m.M24);
            w.Write(m.M31);
            w.Write(m.M32);
            w.Write(m.M33);
            w.Write(m.M34);
            w.Write(m.M41);
            w.Write(m.M42);
            w.Write(m.M43);
            w.Write(m.M44);
        }

        internal static void WriteMatrix43(this BinaryWriter w, Matrix4x4 m)
        {
            w.Write(m.M11);
            w.Write(m.M12);
            w.Write(m.M13);
            w.Write(m.M21);
            w.Write(m.M22);
            w.Write(m.M23);
            w.Write(m.M31);
            w.Write(m.M32);
            w.Write(m.M33);
            w.Write(m.M41);
            w.Write(m.M42);
            w.Write(m.M43);
        }

        internal static void Write(this BinaryWriter w, Vector2 v)
        {
            w.Write(v.X);
            w.Write(v.Y);
        }

        internal static void Write(this BinaryWriter w, Vector3 v)
        {
            w.Write(v.X);
            w.Write(v.Y);
            w.Write(v.Z);
        }

        internal static void Write(this BinaryWriter w, Vector4 v)
        {
            w.Write(v.X);
            w.Write(v.Y);
            w.Write(v.Z);
            w.Write(v.W);
        }

        internal static void Write(this BinaryWriter w, Quaternion q)
        {
            w.Write(q.X);
            w.Write(q.Y);
            w.Write(q.Z);
            w.Write(q.W);
        }

        internal static void Write(this BinaryWriter w, RgbaF c)
        {
            w.Write(c.R);
            w.Write(c.G);
            w.Write(c.B);
            w.Write(c.A);
        }

        internal static void WriteRgba32(this BinaryWriter w, Rgba32 c)
        {
            w.Write(c.ToArgb());
        }

        internal static void WriteFixedString(this BinaryWriter w, string value, int length)
        {
            var field = new byte[length];
            var encoded = Text.Gbk.GetBytes(value ?? string.Empty);
            var n = System.Math.Min(encoded.Length, length);
            System.Array.Copy(encoded, field, n);
            w.Write(field);
        }

        internal static void WriteQuaternionArray(this BinaryWriter w, Quaternion[] a)
        {
            for (var i = 0; i < a.Length; i++)
            {
                w.Write(a[i]);
            }
        }

        internal static void WriteVec2Array(this BinaryWriter w, Vector2[] a)
        {
            for (var i = 0; i < a.Length; i++)
            {
                w.Write(a[i]);
            }
        }

        internal static void WriteVec3Array(this BinaryWriter w, Vector3[] a)
        {
            for (var i = 0; i < a.Length; i++)
            {
                w.Write(a[i]);
            }
        }

        internal static void WriteUintArray(this BinaryWriter w, uint[] a)
        {
            for (var i = 0; i < a.Length; i++)
            {
                w.Write(a[i]);
            }
        }

        internal static void WriteFloats(this BinaryWriter w, float[] a)
        {
            for (var i = 0; i < a.Length; i++)
            {
                w.Write(a[i]);
            }
        }

        internal static void WriteColors(this BinaryWriter w, RgbaF[] a)
        {
            for (var i = 0; i < a.Length; i++)
            {
                w.Write(a[i]);
            }
        }
    }
}
