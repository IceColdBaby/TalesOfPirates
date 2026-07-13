namespace Top.MindPower.Minimap
{
    /// <summary>
    /// Directory node in a .pk container.
    /// <br/> CPackFile::DirectoryData (PackFile.cpp)
    /// </summary>
    public class PkDirectory
    {
        public string Name;
        public PkEntry[] Files;
        public PkDirectory[] Directories;
    }
}
