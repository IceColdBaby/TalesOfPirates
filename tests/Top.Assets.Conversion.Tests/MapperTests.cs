using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Assets.Conversion;
using Top.Gltf;
using Top.MindPower;
using Top.MindPower.Animation;
using Top.MindPower.Geometry;

namespace Top.Assets.Conversion.Tests
{
    public class MapperTests
    {
        private static SceneModel Load()
        {
            using var stream = File.OpenRead(Fixtures.Path("lmo/by-bd015.lmo"));
            return LmoFile.Read(stream).Model;
        }

        [Test]
        public void Maps_all_objects_and_helper_meshes_to_nodes()
        {
            var model = Load();
            var conversion = GeometryObjectGltfMapper.Map(model, "by-bd015");
            var doc = conversion.Document;

            Assert.That(doc.Nodes.Count, Is.EqualTo(23));
            Assert.That(doc.Nodes.Count(n => n.Name.StartsWith("geom_")), Is.EqualTo(22));
            Assert.That(doc.Nodes.Count(n => n.Name == "helper_block_0"), Is.EqualTo(1));
            Assert.That(doc.Meshes.Count, Is.EqualTo(23));
            Assert.That(doc.Scenes[0].Nodes.Count, Is.EqualTo(23), "all by-bd015 nodes are roots");
        }

        [Test]
        public void First_object_keeps_subset_order_as_primitive_order()
        {
            var model = Load();
            var doc = GeometryObjectGltfMapper.Map(model, "by-bd015").Document;
            var mesh = doc.Meshes.First(m => m.Name == "geom_0");

            Assert.That(mesh.Primitives.Count, Is.EqualTo(2));
            Assert.That(mesh.Primitives[0].Attributes, Contains.Key("POSITION"));
            Assert.That(mesh.Primitives[0].Attributes, Contains.Key("NORMAL"));
            Assert.That(mesh.Primitives[0].Attributes, Contains.Key("COLOR_0"));
            Assert.That(mesh.Primitives[0].Attributes, Contains.Key("TEXCOORD_0"));
        }

        [Test]
        public void Positions_swap_up_axis()
        {
            var model = Load();
            var conversion = GeometryObjectGltfMapper.Map(model, "by-bd015");
            var doc = conversion.Document;
            var data = new GltfData(
                new GltfFile { Document = doc, BinChunk = conversion.Bin });

            var obj = model.GeometryObjects[0];
            var accessor = doc.Meshes
                .First(m => m.Name == "geom_0").Primitives[0].Attributes["POSITION"];
            var floats = data.ReadFloats(accessor);

            Assert.That(floats[0], Is.EqualTo(obj.Mesh.Vertices[0].X));
            Assert.That(floats[1], Is.EqualTo(obj.Mesh.Vertices[0].Z));
            Assert.That(floats[2], Is.EqualTo(obj.Mesh.Vertices[0].Y));
        }

        [Test]
        public void Triangle_winding_is_reversed()
        {
            var model = Load();
            var conversion = GeometryObjectGltfMapper.Map(model, "by-bd015");
            var doc = conversion.Document;
            var data = new GltfData(
                new GltfFile { Document = doc, BinChunk = conversion.Bin });

            var obj = model.GeometryObjects[0];
            var subset = obj.Mesh.Subsets[0];
            var primitive = doc.Meshes.First(m => m.Name == "geom_0").Primitives[0];
            var indices = data.ReadInts(primitive.Indices.Value);

            Assert.That(indices[0], Is.EqualTo((int)obj.Mesh.Indices[subset.StartIndex]));
            Assert.That(indices[1], Is.EqualTo((int)obj.Mesh.Indices[subset.StartIndex + 2]));
            Assert.That(indices[2], Is.EqualTo((int)obj.Mesh.Indices[subset.StartIndex + 1]));
        }

        [Test]
        public void Node_matrix_swaps_up_axis()
        {
            var model = Load();
            var doc = GeometryObjectGltfMapper.Map(model, "by-bd015").Document;
            var obj = model.GeometryObjects[0];
            var node = doc.Nodes.First(n => n.Name == "geom_0");

            Assert.That(node.Matrix[0], Is.EqualTo(obj.LocalMatrix.M11));
            Assert.That(node.Matrix[2], Is.EqualTo(obj.LocalMatrix.M12));
            Assert.That(node.Matrix[13], Is.EqualTo(obj.LocalMatrix.M43),
                "up translation lands in glTF y");
            Assert.That(node.Matrix[14], Is.EqualTo(obj.LocalMatrix.M42));
        }

        [Test]
        public void Child_objects_nest_under_their_parent_node()
        {
            var model = new SceneModel
            {
                GeometryObjects = new[]
                {
                    MakeObject(id: 5, parentId: uint.MaxValue),
                    MakeObject(id: 9, parentId: 5),
                },
                Helpers = System.Array.Empty<Helper>(),
            };

            var doc = GeometryObjectGltfMapper.Map(model, "synthetic").Document;

            Assert.That(doc.Scenes[0].Nodes, Is.EqualTo(new[] { 0 }),
                "only the parent is a scene root");
            Assert.That(doc.Nodes[0].Name, Is.EqualTo("geom_5"));
            Assert.That(doc.Nodes[0].Children, Is.EqualTo(new[] { 1 }));
            Assert.That(doc.Nodes[1].Name, Is.EqualTo("geom_9"));
        }

        [Test]
        public void A_repeated_object_id_is_reported()
        {
            var model = new SceneModel
            {
                GeometryObjects = new[]
                {
                    MakeObject(id: 4, parentId: uint.MaxValue),
                    MakeObject(id: 4, parentId: uint.MaxValue),
                },
                Helpers = System.Array.Empty<Helper>(),
            };

            var conversion = GeometryObjectGltfMapper.Map(model, "synthetic");

            // Five .lmo files in the corpus repeat an id. The name is the only
            // handle the scaffolder has, so the collision has to be visible.
            Assert.That(conversion.Warnings, Has.Some.Contains("object id 4 occurs more than once"));
            Assert.That(conversion.Document.Nodes.Count, Is.EqualTo(2),
                "both objects are still emitted; only their bindings collide");
        }

        [Test]
        public void Matrix_animations_become_per_node_gltf_animations()
        {
            var model = Load();
            var conversion = GeometryObjectGltfMapper.Map(model, "by-bd015");
            var doc = conversion.Document;

            Assert.That(conversion.Warnings, Is.Empty);
            Assert.That(doc.Animations, Has.Count.EqualTo(11));

            var geom3 = doc.Nodes.FindIndex(n => n.Name == "geom_3");
            var animation = doc.Animations.Find(a => a.Name == "geom_3");
            Assert.That(animation, Is.Not.Null);
            Assert.That(animation.Channels, Has.Count.EqualTo(3));
            Assert.That(animation.Channels.TrueForAll(c => c.Target.Node == geom3));

            var rotationChannel = animation.Channels.Find(c => c.Target.Path == "rotation");
            var sampler = animation.Samplers[rotationChannel.Sampler];
            Assert.That(doc.Accessors[sampler.Input].Count, Is.EqualTo(701));
            Assert.That(doc.Accessors[sampler.Input].Max[0],
                Is.EqualTo(700f / 30f).Within(0.001f));

            var pendulum = doc.Animations.Find(a => a.Name == "geom_13");
            var pendulumInput = pendulum.Samplers[pendulum.Channels[0].Sampler].Input;
            Assert.That(doc.Accessors[pendulumInput].Max[0],
                Is.EqualTo(60f / 30f).Within(0.001f), "pendulum loops at its own 2s cycle");
        }

        [Test]
        public void Animated_nodes_use_trs_instead_of_matrix()
        {
            var model = Load();
            var doc = GeometryObjectGltfMapper.Map(model, "by-bd015").Document;
            var node = doc.Nodes.First(n => n.Name == "geom_3");

            Assert.That(node.Matrix, Is.Null);
            Assert.That(node.Translation, Is.Not.Null);
            Assert.That(node.Rotation, Is.Not.Null);
            Assert.That(node.Scale, Is.Not.Null);
        }

        [Test]
        public void Preview_materials_reference_shared_textures()
        {
            var model = Load();
            var doc = GeometryObjectGltfMapper.Map(model, "by-bd015", "../../Textures").Document;

            Assert.That(doc.Materials, Is.Not.Null);
            Assert.That(doc.Images.TrueForAll(
                i => i.Uri.StartsWith("../../Textures/") && i.Uri.EndsWith(".png")));

            var mesh = doc.Meshes.Find(m => m.Name == "geom_0");
            Assert.That(mesh.Primitives.TrueForAll(p => p.Material != null));
            Assert.That(doc.Materials[mesh.Primitives[0].Material.Value].Name,
                Is.EqualTo("by-bd015_0_0"));

            Assert.That(doc.Materials.Select(m => m.Name), Is.Unique);
            Assert.That(doc.Materials.Exists(m => m.AlphaMode == "MASK"));
            Assert.That(doc.Materials.Exists(m => m.AlphaMode == "BLEND"));
        }

        [Test]
        public void Geometry_only_output_has_no_materials()
        {
            var model = Load();
            var doc = GeometryObjectGltfMapper.Map(model, "by-bd015").Document;

            Assert.That(doc.Materials, Is.Null);
        }

        [Test]
        public void Single_object_maps_to_one_root_node()
        {
            var doc = GeometryObjectGltfMapper
                .Map(MakeObject(id: 5, parentId: uint.MaxValue), "single").Document;

            Assert.That(doc.Nodes, Has.Count.EqualTo(1));
            Assert.That(doc.Nodes[0].Name, Is.EqualTo("geom_5"));
            Assert.That(doc.Scenes[0].Nodes, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Skinning_data_is_reported()
        {
            var obj = MakeObject(id: 1, parentId: uint.MaxValue);
            obj.Mesh.SkinBlends = new SkinBlend[3];

            var conversion = GeometryObjectGltfMapper.Map(obj, "skinned");

            Assert.That(conversion.Warnings, Has.Some.Contains("skinning"));
        }

        [Test]
        public void Helper_dummies_become_locator_nodes()
        {
            var obj = MakeObject(id: 1, parentId: uint.MaxValue);
            obj.Helper = new Helper
            {
                Dummies = new[]
                {
                    new HelperDummy
                    {
                        Id = 4,
                        Matrix = System.Numerics.Matrix4x4.CreateTranslation(1f, 2f, 3f),
                    },
                },
            };

            var doc = GeometryObjectGltfMapper.Map(obj, "dummies").Document;
            var node = doc.Nodes.Find(n => n.Name == "dummy_4");

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Mesh, Is.Null);
            Assert.That(doc.Scenes[0].Nodes, Has.Member(doc.Nodes.IndexOf(node)));
            Assert.That(node.Matrix[12], Is.EqualTo(1f));
            Assert.That(node.Matrix[13], Is.EqualTo(3f), "up translation lands in glTF y");
            Assert.That(node.Matrix[14], Is.EqualTo(2f));
        }

        [Test]
        public void Helper_meshes_carry_their_source_name()
        {
            var obj = MakeObject(id: 1, parentId: uint.MaxValue);
            obj.Helper = new Helper
            {
                Meshes = new[]
                {
                    MakeHelperMesh("Block"),
                    MakeHelperMesh(null),
                },
            };

            var doc = GeometryObjectGltfMapper.Map(obj, "helpers").Document;

            Assert.That(doc.Nodes.Exists(n => n.Name == "helper_block_0"), Is.True);
            Assert.That(doc.Nodes.Exists(n => n.Name == "helper_mesh_1"), Is.True);
        }

        [Test]
        public void Bone_animation_without_blends_warns()
        {
            var obj = MakeObject(id: 2, parentId: uint.MaxValue);
            obj.Animation = new AnimationData
            {
                Bone = new BoneAnimation(),
                MaterialOpacity = new[] { new MaterialOpacityAnimation() },
            };

            var warnings = GeometryObjectGltfMapper.Map(obj, "animated").Warnings;

            Assert.That(warnings, Has.Some.Contains("bone animation"));
            Assert.That(warnings, Has.None.Contains("opacity"));
        }

        [Test]
        public void Non_list_primitive_type_warns()
        {
            var obj = MakeObject(id: 6, parentId: uint.MaxValue);
            obj.Mesh.PointType = 5;

            var warnings = GeometryObjectGltfMapper.Map(obj, "strip").Warnings;

            Assert.That(warnings, Has.Some.Contains("primitive type 5"));
        }

        [Test]
        public void Degenerate_normals_are_replaced()
        {
            var obj = MakeObject(id: 8, parentId: uint.MaxValue);
            obj.Mesh.Normals = new[]
            {
                new System.Numerics.Vector3(float.NaN, 0, 0),
                System.Numerics.Vector3.Zero,
                new System.Numerics.Vector3(2, 0, 0),
            };

            var conversion = GeometryObjectGltfMapper.Map(obj, "normals");
            var doc = conversion.Document;
            var data = new GltfData(
                new GltfFile { Document = doc, BinChunk = conversion.Bin });
            var accessor = doc.Meshes[0].Primitives[0].Attributes["NORMAL"];
            var floats = data.ReadFloats(accessor);

            Assert.That(conversion.Warnings, Has.Some.Contains("2 degenerate normals"));
            // NaN and zero-length normals fall back to source-space up, which
            // the axis swap turns into glTF (0, 1, 0); valid ones renormalize.
            Assert.That(floats, Is.EqualTo(new[]
            {
                0f, 1f, 0f,
                0f, 1f, 0f,
                1f, 0f, 0f,
            }));
        }

        [Test]
        public void Lit_subset_splits_into_child_node()
        {
            var obj = MakeObject(id: 7, parentId: uint.MaxValue);
            obj.Mesh.Indices = new uint[] { 0, 1, 2, 0, 1, 2 };
            obj.Mesh.Subsets = new[]
            {
                new MeshSubset { PrimitiveCount = 1, StartIndex = 0 },
                new MeshSubset { PrimitiveCount = 1, StartIndex = 3 },
            };

            var doc = GeometryObjectGltfMapper.Map(obj, "weapon", litSubset: 1).Document;

            Assert.That(doc.Nodes, Has.Count.EqualTo(2));
            Assert.That(doc.Nodes[0].Name, Is.EqualTo("geom_7"));
            Assert.That(doc.Nodes[0].Children, Is.EqualTo(new[] { 1 }));
            Assert.That(doc.Nodes[1].Name, Is.EqualTo("geom_7_lit"));
            Assert.That(doc.Scenes[0].Nodes, Is.EqualTo(new[] { 0 }), "lit node is not a scene root");
            Assert.That(doc.Meshes[0].Primitives, Has.Count.EqualTo(1));
            Assert.That(doc.Meshes[1].Primitives, Has.Count.EqualTo(1));
        }

        [Test]
        public void Lit_subset_needs_a_second_subset()
        {
            var doc = GeometryObjectGltfMapper
                .Map(MakeObject(id: 3, parentId: uint.MaxValue), "single", litSubset: 0).Document;

            Assert.That(doc.Nodes, Has.Count.EqualTo(1));
            Assert.That(doc.Meshes, Has.Count.EqualTo(1));
            Assert.That(doc.Meshes[0].Primitives, Has.Count.EqualTo(1));
        }

        [Test]
        public void Static_opacity_emits_blend_and_a_base_color_factor()
        {
            var obj = MakeObject(0, uint.MaxValue);
            obj.Materials = new[]
            {
                new MaterialTexture { Transparency = TransparencyType.Filter, Opacity = 0.3f },
            };

            var material = GeometryObjectGltfMapper
                .Map(obj, "translucent", "../Textures").Document.Materials[0];

            Assert.That(material.AlphaMode, Is.EqualTo("BLEND"));
            Assert.That(material.PbrMetallicRoughness.BaseColorFactor, Has.Length.EqualTo(4));
            Assert.That(material.PbrMetallicRoughness.BaseColorFactor[3],
                Is.EqualTo(0.3f).Within(1e-6f));
        }

        [Test]
        public void Opaque_materials_carry_no_base_color_factor()
        {
            var obj = MakeObject(0, uint.MaxValue);
            obj.Materials = new[] { new MaterialTexture { Transparency = TransparencyType.Filter } };

            var material = GeometryObjectGltfMapper
                .Map(obj, "opaque", "../Textures").Document.Materials[0];

            Assert.That(material.AlphaMode, Is.Null);
            Assert.That(material.PbrMetallicRoughness.BaseColorFactor, Is.Null);
        }

        [Test]
        public void An_opacity_track_emits_blend_at_full_opacity()
        {
            var obj = MakeObject(0, uint.MaxValue);
            obj.Materials = new[]
            {
                new MaterialTexture { Transparency = TransparencyType.Filter, Opacity = 1f },
            };
            obj.Animation = new AnimationData
            {
                MaterialOpacity = new[]
                {
                    new MaterialOpacityAnimation { Keys = new FloatKeyframe[2] },
                },
            };

            var material = GeometryObjectGltfMapper
                .Map(obj, "animated", "../Textures").Document.Materials[0];

            Assert.That(material.AlphaMode, Is.EqualTo("BLEND"));
            Assert.That(material.PbrMetallicRoughness.BaseColorFactor, Is.Null,
                "the stored opacity is still one; only the track moves it");
        }

        [Test]
        public void Alpha_blend_atom_on_a_filter_material_emits_blend()
        {
            var obj = MakeObject(0, uint.MaxValue);
            obj.Materials = new[]
            {
                new MaterialTexture
                {
                    Transparency = TransparencyType.Filter,
                    RenderStates = new[] { new RenderStateAtom { State = 27, Value0 = 1 } },
                },
            };

            var material = GeometryObjectGltfMapper
                .Map(obj, "blended", "../Textures").Document.Materials[0];

            Assert.That(material.AlphaMode, Is.EqualTo("BLEND"));
        }

        [Test]
        public void Render_state_warnings_reach_the_conversion()
        {
            var obj = MakeObject(0, uint.MaxValue);
            obj.Materials = new[]
            {
                new MaterialTexture
                {
                    Transparency = TransparencyType.Filter,
                    RenderStates = new[] { new RenderStateAtom { State = 999, Value0 = 1 } },
                },
            };

            var conversion = GeometryObjectGltfMapper.Map(obj, "warned", "../Textures");

            Assert.That(conversion.Warnings,
                Has.Some.EqualTo("warned_0_0: unmapped render state 999=1"));
        }

        private static GeometryObject MakeObject(uint id, uint parentId)
        {
            return new GeometryObject
            {
                Id = id,
                ParentId = parentId,
                LocalMatrix = System.Numerics.Matrix4x4.Identity,
                Mesh = new Mesh
                {
                    PointType = 4,
                    Vertices = new[]
                    {
                        new System.Numerics.Vector3(0, 0, 0),
                        new System.Numerics.Vector3(1, 0, 0),
                        new System.Numerics.Vector3(0, 1, 0),
                    },
                    Indices = new uint[] { 0, 1, 2 },
                    Subsets = new[] { new MeshSubset { PrimitiveCount = 1, StartIndex = 0 } },
                },
            };
        }

        private static HelperMesh MakeHelperMesh(string name)
        {
            return new HelperMesh
            {
                Name = name,
                Matrix = System.Numerics.Matrix4x4.Identity,
                Vertices = new[]
                {
                    new System.Numerics.Vector3(0, 0, 0),
                    new System.Numerics.Vector3(1, 0, 0),
                    new System.Numerics.Vector3(0, 1, 0),
                },
                Faces = new[]
                {
                    new HelperMeshFace { Vertex = new uint[] { 0, 1, 2 } },
                },
            };
        }
    }
}
