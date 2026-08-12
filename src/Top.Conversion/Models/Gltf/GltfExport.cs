using System;
using Top.Conversion.Gltf;
using Top.Legacy.MindPower.Animation;
using Top.Legacy.MindPower.Geometry;
using Top.Legacy.Tables.Custom;

namespace Top.Conversion.Models.Gltf
{
    /// <summary>
    /// Turns one of the original client's model shapes into a glTF document.
    /// Each entry point takes the sources of that shape and hands back the
    /// finished file.
    /// </summary>
    public static class GltfExport
    {
        public static GltfFile Character(string name, BoneAnimation skeleton, GeometryObject[] parts,
            CharacterAction[] actions, string textureUriPrefix = null, bool emitClips = true)
        {
            var builder = new ModelGltfBuilder(name, textureUriPrefix);

            builder.AddRigged(skeleton, parts, actions, emitClips);

            return builder.Build();
        }

        public static GltfFile Rig(string name, BoneAnimation skeleton, CharacterAction[] actions)
        {
            var builder = new ModelGltfBuilder(name);

            builder.AddRigged(skeleton, Array.Empty<GeometryObject>(), actions, emitClips: true);

            return builder.Build();
        }

        public static GltfFile Model(string name, SceneModel model, string textureUriPrefix = null)
        {
            var builder = new ModelGltfBuilder(name, textureUriPrefix);

            builder.AddObjects(model.GeometryObjects, model.Helpers);

            return builder.Build();
        }

        public static GltfFile Object(string name, GeometryObject obj, string textureUriPrefix = null,
            int? litSubset = null)
        {
            var builder = new ModelGltfBuilder(name, textureUriPrefix, litSubset);

            builder.AddObject(obj);

            return builder.Build();
        }

        public static GltfFile Object(string name, GeometryObject obj, BoneAnimation skeleton,
            string textureUriPrefix = null)
        {
            var builder = new ModelGltfBuilder(name, textureUriPrefix);

            builder.AddRiggedObject(obj, skeleton);

            return builder.Build();
        }
    }
}
