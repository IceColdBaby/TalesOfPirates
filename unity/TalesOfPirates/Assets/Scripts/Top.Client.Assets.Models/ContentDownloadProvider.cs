using System;
using System.Threading.Tasks;
using GLTFast.Loading;
using Top.Content.Packs;

namespace Top.Client.Assets.Models
{
    /// <summary>
    /// Downloads model and texture data from a composed content source.
    /// Implements the IDownloadProvider interface for handling GLTF asset downloads.
    /// </summary>
    public class ContentDownloadProvider : IDownloadProvider
    {
        private const string Scheme = "top";

        private static readonly Uri Root = new Uri(Scheme + "://content/");

        private readonly IComposedContent _content;

        public ContentDownloadProvider(IComposedContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public static Uri UriFor(string path)
        {
            return new Uri(Root, path);
        }

        public async Task<IDownload> Request(Uri url)
        {
            return await Fetch(url, false);
        }

        public async Task<ITextureDownload> RequestTexture(Uri url, bool nonReadable)
        {
            return await Fetch(url, nonReadable);
        }

        private async Task<ContentDownload> Fetch(Uri url, bool nonReadable)
        {
            var path = PathOf(url);

            if (path == null)
            {
                return new ContentDownload($"'{url}' is not a content URI");
            }

            try
            {
                return new ContentDownload(await _content.Read(path), nonReadable);
            }
            catch (Exception exception)
            {
                return new ContentDownload(exception.Message);
            }
        }

        private static string PathOf(Uri url)
        {
            if (url == null || !url.IsAbsoluteUri || url.Scheme != Scheme)
            {
                return null;
            }

            var path = Uri.UnescapeDataString(url.AbsolutePath).TrimStart('/');

            return path.Length == 0 ? null : path;
        }
    }
}
