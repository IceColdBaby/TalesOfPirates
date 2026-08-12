namespace Top.Legacy.MindPower.Minimap
{
    /// <summary>
    /// One packed file inside a .pk container.
    /// <br/> CPackFile::FileData (PackFile.cpp)
    /// </summary>
    public class PkEntry
    {
        public string Name;
        public byte[] Payload;
    }
}
