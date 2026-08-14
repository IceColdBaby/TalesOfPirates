using System.Text;
using GLTFast;
using GLTFast.Loading;
using UnityEngine;

namespace Top.Client.Assets.Models.Gltf
{
    /// <summary>
    /// Downloadable content object that encapsulates data, errors, and other relevant information
    /// for managing assets such as textures and text content.
    /// Implements the <see cref="ITextureDownload"/> interface, providing functionality to manage texture data.
    /// </summary>
    public class ContentDownload : ITextureDownload
    {
        private readonly bool _nonReadable;

        private Texture2D _texture;

        public ContentDownload(byte[] data, bool nonReadable = false)
        {
            Data = data;
            _nonReadable = nonReadable;
        }

        public ContentDownload(string error)
        {
            Error = error;
        }

        public bool Success => Error == null;

        public string Error { get; }

        public byte[] Data { get; }

        public string Text => Data != null ? Encoding.UTF8.GetString(Data) : null;

        public bool? IsBinary => Data != null && Data.Length >= 4
            ? GltfGlobals.IsGltfBinary(Data)
            : null;

        public Texture2D Texture
        {
            get
            {
                if (_texture == null && Data != null)
                {
                    _texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    _texture.LoadImage(Data, _nonReadable);
                }

                return _texture;
            }
        }

        public void Dispose()
        {
        }
    }
}
