using System.Collections.Generic;

namespace NBG.NodeGraph
{
    public static class QuickAddNodeMenu
    {
        public enum Folder
        {
            Primitives,
            Physics,
            Aesthetics,
            Utils,
            Math,
            Gameplay,
            LevelSpecific,
            Sound,
            Obsolete,
            MAX,
        }

        public static Dictionary<Folder, string> folderNameOverrides = new Dictionary<Folder, string>()
        {
            { Folder.LevelSpecific, "Level" },
        };



        public static string FolderName(Folder f)
        {
            if (folderNameOverrides.ContainsKey(f))
                return folderNameOverrides[f];
            return f.ToString();
        }
        public static string GetName(Folder f, string l)
        {
            if (string.IsNullOrWhiteSpace(l))
                return FolderName(f) + "/";

            return FolderName(f) + "/" + l + "/";
        }

        public static string GetNameFromAttribute(AddNodeMenuItem attribute)
        {
            if (attribute.subFolder != "")
                return GetName(attribute.folder, attribute.level) + attribute.subFolder + "/";
            return GetName(attribute.folder, attribute.level);
        }
    }
}
