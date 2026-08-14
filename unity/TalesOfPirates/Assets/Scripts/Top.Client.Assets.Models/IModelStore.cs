using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Top.Client.Assets.Models
{
    /// <summary>
    /// Defines a contract for managing model assets, including loading, instantiation,
    /// and preloading operations. The interface provides functionality for efficient
    /// and organized management of model lifecycles within applications.
    /// </summary>
    public interface IModelStore
    {
        Task<ModelInstance> Spawn(string path, Transform parent, CancellationToken cancellationToken = default);
        Task Preload(IEnumerable<string> paths);
    }
}
