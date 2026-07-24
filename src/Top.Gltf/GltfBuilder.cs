using System.Collections.Generic;
using System.Numerics;

namespace Top.Gltf
{
    /// <summary>
    /// Assembles a document's node graph, meshes, materials, skins and
    /// animations, and owns the buffer alongside them. Callers name what they
    /// want rather than where it lands: every add returns a reference, and
    /// nothing outside this file touches a list position.
    /// The element types stay out of reach for a reason - parenting through
    /// <see cref="GltfNodeRef.AddChild"/> appends where assigning a child list
    /// directly would silently discard the children a node already had.
    /// Lists are created only when something goes in them, because glTF
    /// rejects empty arrays.
    /// </summary>
    public sealed class GltfBuilder
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

        /// <summary>
        /// Accessors, which are already addressed by index everywhere in the
        /// format and carry no ownership of their own.
        /// </summary>
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

        /// <summary>
        /// An image and the texture that samples it, which this format always
        /// pairs one to one. A uri already added returns the same texture.
        /// </summary>
        public GltfTextureRef AddTexture(string uri)
        {
            if (_texturesByUri.TryGetValue(uri, out var existing))
            {
                return new GltfTextureRef(existing);
            }

            Document.Images ??= new List<GltfImage>();
            Document.Textures ??= new List<GltfTexture>();
            Document.Images.Add(new GltfImage { Uri = uri });
            Document.Textures.Add(new GltfTexture { Source = Document.Images.Count - 1 });
            _texturesByUri[uri] = Document.Textures.Count - 1;

            return new GltfTextureRef(Document.Textures.Count - 1);
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

        /// <summary>
        /// Animations that never received a channel are dropped here rather
        /// than by their author: glTF rejects them, and nothing addresses an
        /// animation by index.
        /// </summary>
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

    public sealed class GltfNodeRef
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

        public GltfNodeRef WithTrs(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            var node = _builder.Node(Index);

            node.Translation = new[] { translation.X, translation.Y, translation.Z };
            node.Rotation = new[] { rotation.X, rotation.Y, rotation.Z, rotation.W };
            node.Scale = new[] { scale.X, scale.Y, scale.Z };

            return this;
        }

        /// <summary>
        /// Appends. A node keeps every child it was given, whatever order the
        /// caller adds them in.
        /// </summary>
        public GltfNodeRef AddChild(GltfNodeRef child)
        {
            var node = _builder.Node(Index);

            node.Children ??= new List<int>();
            node.Children.Add(child.Index);

            return this;
        }

        /// <summary>
        /// Puts this node in the scene. Only nodes with no parent belong here.
        /// </summary>
        public GltfNodeRef AsRoot()
        {
            _builder.AddRoot(Index);

            return this;
        }

        /// <summary>
        /// Whether this node carries a matrix, which glTF makes exclusive with
        /// the TRS channels an animation targets.
        /// </summary>
        public bool HasMatrix => _builder.Node(Index).Matrix != null;
    }

    public sealed class GltfMeshRef
    {
        private readonly GltfBuilder _builder;

        internal GltfMeshRef(GltfBuilder builder, int index)
        {
            _builder = builder;
            Index = index;
        }

        public int Index { get; }

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

    public sealed class GltfMaterialRef
    {
        private readonly GltfBuilder _builder;

        internal GltfMaterialRef(GltfBuilder builder, int index)
        {
            _builder = builder;
            Index = index;
        }

        public int Index { get; }

        private GltfMaterial Material => _builder.Document.Materials[Index];

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

    public sealed class GltfTextureRef
    {
        internal GltfTextureRef(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }

    public sealed class GltfSkinRef
    {
        private readonly GltfBuilder _builder;

        internal GltfSkinRef(GltfBuilder builder, int index)
        {
            _builder = builder;
            Index = index;
        }

        public int Index { get; }

        public GltfSkinRef WithSkeletonRoot(GltfNodeRef root)
        {
            _builder.Document.Skins[Index].Skeleton = root.Index;

            return this;
        }
    }

    public sealed class GltfAnimationRef
    {
        private readonly GltfBuilder _builder;

        internal GltfAnimationRef(GltfBuilder builder, int index)
        {
            _builder = builder;
            Index = index;
        }

        public int Index { get; }

        public int ChannelCount => _builder.Document.Animations[Index].Channels.Count;

        /// <summary>
        /// One sampler per channel, which is what a track-per-node conversion
        /// produces; sharing an input accessor across channels is the caller's
        /// business.
        /// </summary>
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
