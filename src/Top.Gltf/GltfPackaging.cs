namespace Top.Gltf
{
    /// <summary>
    /// On-disk layout of a written glTF asset.
    /// </summary>
    public enum GltfPackaging
    {
        /// <summary>
        /// Single binary .glb container.
        /// </summary>
        Glb,

        /// <summary>
        /// .gltf JSON carrying the buffer as a base64 data URI.
        /// </summary>
        GltfEmbedded,

        /// <summary>
        /// .gltf JSON next to a separate .bin buffer file.
        /// </summary>
        GltfWithBin,
    }
}
