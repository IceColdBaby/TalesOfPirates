using System.Collections.Generic;
using Top.MindPower.Geometry;

namespace Top.Assets.Conversion.Materials
{
    /// <summary>
    /// A material's render state once the sparse atoms and the transparency
    /// defaults have been reconciled, in the source engine's own D3D terms.
    /// Every field is stated: the source leaves most of them to defaults that
    /// depend on the transparency type.
    /// </summary>
    public sealed class ResolvedRenderState
    {
        public string TextureFile;
        public D3DBlend SrcBlend = D3DBlend.One;
        public D3DBlend DstBlend = D3DBlend.Zero;
        public bool BlendEnabled;
        public bool ZWrite = true;
        public D3DCull Cull = D3DCull.Ccw;
        public bool AlphaTest;
        public float Cutoff = 0.5f;
        public bool Lit = true;
        public float Opacity = 1f;
        public bool UvAnimated;
        public bool OpacityAnimated;
        public TransparencyType Transparency;
        public readonly List<string> Warnings = new List<string>();

        /// <summary>
        /// True when opacity shapes rendering: an animated track or a static
        /// value below one. The engine checks opacity against one the same
        /// way regardless of animation.
        /// - lwMtlTexAgent::BeginSet (lwResourceMgr.cpp)
        /// </summary>
        public bool OpacityDriven => OpacityAnimated || Opacity != 1f;
    }
}
