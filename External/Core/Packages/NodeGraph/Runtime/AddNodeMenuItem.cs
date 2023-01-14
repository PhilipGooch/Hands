namespace NBG.NodeGraph
{
    public class AddNodeMenuItem : System.Attribute
    {
        public QuickAddNodeMenu.Folder folder;
        public string level = null;
        public string subFolder = "";
        public AddNodeMenuItem(QuickAddNodeMenu.Folder folder, string level = null, string subFolder = "")
        {
            this.folder = folder;
            this.level = level;
            this.subFolder = subFolder;
        }
    }
}
