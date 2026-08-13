using System.Collections.Generic;
using System.Threading.Tasks;

namespace Top.Content.Packs
{
    /// <summary>
    /// Represents composed content, which provides functionality for reading,
    /// determining the existence of, and listing resources organized by path.
    /// </summary>
    public interface IComposedContent
    {
        Task<byte[]> Read(string path);

        bool Exists(string path);

        IEnumerable<string> List(string prefix);
    }
}
