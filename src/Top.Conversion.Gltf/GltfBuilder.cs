using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace Top.Conversion.Gltf
{
    /// <summary>
    /// Fluent builder for a glTF document and its binary buffer. Nodes, meshes,
    /// materials, textures, skins and animations are added by name and returned
    /// as refs that carry the chained setters. Textures dedupe by URI and
    /// animations with no channels drop at <see cref="Build"/>.
    /// </summary>
    public class GltfBuilder
    {
        private readonly GltfScene _scene;
        private readonly Dictionary<string, int> _texturesByUri = new Dictionary<string, int>();

        public GltfBuilder(string sceneName, string generator = null)
        {
            Document = new GltfDocument
            {
                Asset = { Generator = generator },
                Scene = 0,
                Scenes = new List<GltfScene>(),
            };
            _scene = new GltfScene { Name = sceneName, Nodes = new List<int>() };
            Document.Scenes.Add(_scene);
            Buffer = new BufferBuilder(Document);
        }

        public GltfDocument Document { get; }

        public BufferBuilder Buffer { get; }

        public GltfNodeRef AddNode(string name)
        {
            Document.Nodes ??= new List<GltfNode>();
            Document.Nodes.Add(new GltfNode { Name = name });

            return new GltfNodeRef(this, Document.Nodes.Count - 1);
        }

        public GltfMeshRef AddMesh(string name)
        {
            Document.Meshes ??= new List<GltfMesh>();
            Document.Meshes.Add(new GltfMesh { Name = name });

            return new GltfMeshRef(this, Document.Meshes.Count - 1);
        }

        public GltfMaterialRef AddMaterial(string name)
        {
            Document.Materials ??= new List<GltfMaterial>();
            Document.Materials.Add(new GltfMaterial
            {
                Name = name,
                PbrMetallicRoughness = new GltfPbrMetallicRoughness { MetallicFactor = 0f },
            });

            return new GltfMaterialRef(this, Document.Materials.Count - 1);
        }

        public GltfTextureRef AddTexture(string uri)
        {
            if (_texturesByUri.TryGetValue(uri, out var existing))
            {
                return new GltfTextureRef(this, existing);
            }

            Document.Images ??= new List<GltfImage>();
            Document.Textures ??= new List<GltfTexture>();
            Document.Images.Add(new GltfImage { Uri = uri });
            Document.Textures.Add(new GltfTexture { Source = Document.Images.Count - 1 });
            _texturesByUri[uri] = Document.Textures.Count - 1;

            return new GltfTextureRef(this, Document.Textures.Count - 1);
        }

        public GltfSkinRef AddSkin(string name, IEnumerable<GltfNodeRef> joints, int inverseBindMatrices)
        {
            var skin = new GltfSkin { Name = name, InverseBindMatrices = inverseBindMatrices };

            foreach (var joint in joints)
            {
                skin.Joints.Add(joint.Index);
            }

            Document.Skins ??= new List<GltfSkin>();
            Document.Skins.Add(skin);

            return new GltfSkinRef(this, Document.Skins.Count - 1);
        }

        public GltfAnimationRef AddAnimation(string name)
        {
            Document.Animations ??= new List<GltfAnimation>();
            Document.Animations.Add(new GltfAnimation { Name = name });

            return new GltfAnimationRef(this, Document.Animations.Count - 1);
        }

        public GltfFile Build()
        {
            if (Document.Animations != null)
            {
                Document.Animations.RemoveAll(a => a.Channels.Count == 0);

                if (Document.Animations.Count == 0)
                {
                    Document.Animations = null;
                }
            }

            return new GltfFile { Document = Document, BinChunk = Buffer.Finish() };
        }

        internal GltfNode Node(int index)
        {
            return Document.Nodes[index];
        }

        internal void AddRoot(int index)
        {
            _scene.Nodes.Add(index);
        }
    }

    public class GltfNodeRef
    {
        private readonly GltfBuilder _builder;

        internal GltfNodeRef(GltfBuilder builder, int index)
        {
            _builder = builder;
            Index = index;
        }

        public int Index { get; }

        public string Name => _builder.Node(Index).Name;

        public GltfNodeRef WithMesh(GltfMeshRef mesh)
        {
            _builder.Node(Index).Mesh = mesh.Index;

            return this;
        }

        public GltfNodeRef WithSkin(GltfSkinRef skin)
        {
            _builder.Node(Index).Skin = skin.Index;

            return this;
        }

        public GltfNodeRef WithMatrix(float[] matrix)
        {
            _builder.Node(Index).Matrix = matrix;

            return this;
        }

        public GltfNodeRef WithExtras(JToken extras)
        {
            _builder.Node(Index).Extras = extras;

            return this;
        }

        public GltfNodeRef WithTrs(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            var node = _builder.Node(Index);

            node.Translation = new[] { translation.X, translation.Y, translation.Z };
            node.Rotation = new[] { rotation.X, rotation.Y, rotation.Z, rotation.W };
            node.Scale = new[] { scale.X, scale.Y, scale.Z };

            return this;
        }

        public GltfNodeRef AddChild(GltfNodeRef child)
        {
            var node = _builder.Node(Index);

            node.Children ??= new List<int>();
            node.Children.Add(child.Index);

            return this;
        }

        public GltfNodeRef AsRoot()
        {
            _builder.AddRoot(Index);

            return this;
        }

        public bool HasMatrix => _builder.Node(Index).Matrix != null;
    }

    public class GltfMeshRef
    {
        private readonly GltfBuilder _builder;

        internal GltfMeshRef(GltfBuilder builder, int index)
        {
            _builder = builder;
            Index = index;
        }

        public int Index { get; }

        public GltfMeshRef WithExtras(JToken extras)
        {
            _builder.Document.Meshes[Index].Extras = extras;

            return this;
        }

        public GltfMeshRef AddPrimitive(Dictionary<string, int> attributes, int indices,
            GltfMaterialRef material = null)
        {
            _builder.Document.Meshes[Index].Primitives.Add(new GltfPrimitive
            {
                Attributes = attributes,
                Indices = indices,
                Material = material?.Index,
            });

            return this;
        }
    }

    public class GltfMaterialRef
    {
        private readonly GltfBuilder _builder;

        internal GltfMaterialRef(GltfBuilder builder, int index)
        {
            _builder = builder;
            Index = index;
        }

        public int Index { get; }

        private GltfMaterial Material => _builder.Document.Materials[Index];

        public GltfMaterialRef WithExtras(JToken extras)
        {
            Material.Extras = extras;

            return this;
        }

        public GltfMaterialRef WithTexture(GltfTextureRef texture)
        {
            Material.PbrMetallicRoughness.BaseColorTexture =
                new GltfTextureInfo { Index = texture.Index };

            return this;
        }

        public GltfMaterialRef WithBaseColorFactor(float[] factor)
        {
            Material.PbrMetallicRoughness.BaseColorFactor = factor;

            return this;
        }

        public GltfMaterialRef WithDoubleSided(bool doubleSided)
        {
            Material.DoubleSided = doubleSided;

            return this;
        }

        public GltfMaterialRef WithAlphaBlend()
        {
            Material.AlphaMode = "BLEND";

            return this;
        }

        public GltfMaterialRef WithAlphaMask(float cutoff)
        {
            Material.AlphaMode = "MASK";
            Material.AlphaCutoff = cutoff;

            return this;
        }
    }

    public class GltfTextureRef
    {
        private readonly GltfBuilder _builder;

        internal GltfTextureRef(GltfBuilder builder, int index)
        {
            _builder = builder;
            Index = index;
        }

        public int Index { get; }

        public GltfTextureRef WithExtras(JToken extras)
        {
            _builder.Document.Textures[Index].Extras = extras;

            return this;
        }
    }

    public class GltfSkinRef
    {
        private readonly GltfBuilder _builder;

        internal GltfSkinRef(GltfBuilder builder, int index)
        {
            _builder = builder;
            Index = index;
        }

        public int Index { get; }

        public GltfSkinRef WithExtras(JToken extras)
        {
            _builder.Document.Skins[Index].Extras = extras;

            return this;
        }

        public GltfSkinRef WithSkeletonRoot(GltfNodeRef root)
        {
            _builder.Document.Skins[Index].Skeleton = root.Index;

            return this;
        }
    }

    public class GltfAnimationRef
    {
        private readonly GltfBuilder _builder;

        internal GltfAnimationRef(GltfBuilder builder, int index)
        {
            _builder = builder;
            Index = index;
        }

        public int Index { get; }

        public int ChannelCount => _builder.Document.Animations[Index].Channels.Count;

        public GltfAnimationRef WithExtras(JToken extras)
        {
            _builder.Document.Animations[Index].Extras = extras;

            return this;
        }

        public GltfAnimationRef AddChannel(int input, int output, GltfNodeRef target, string path)
        {
            var animation = _builder.Document.Animations[Index];

            animation.Samplers.Add(new GltfAnimationSampler
            {
                Input = input,
                Output = output,
                Interpolation = "LINEAR",
            });

            animation.Channels.Add(new GltfAnimationChannel
            {
                Sampler = animation.Samplers.Count - 1,
                Target = { Node = target.Index, Path = path },
            });

            return this;
        }
    }
}
