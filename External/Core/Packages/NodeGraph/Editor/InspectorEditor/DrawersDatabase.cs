using System;
using System.Collections.Generic;

namespace NBG.NodeGraph.Editor
{
    static class AttributeDatabase
    {
        private static Dictionary<Type, NBGPropertyDrawer> drawersByAttributeType;
        private static Dictionary<Type, NBGPropertyDrawer> metaByAttributeType;

        static AttributeDatabase()
        {
            drawersByAttributeType = new Dictionary<Type, NBGPropertyDrawer>();
            drawersByAttributeType[typeof(MinMaxSliderAttribute)] = new MinMaxSliderPropertyDrawer();
            drawersByAttributeType[typeof(ReadOnlyAttribute)] = new ReadOnlyPropertyDrawer();
            drawersByAttributeType[typeof(RenameAttribute)] = new RenamePropertyDrawer();

            metaByAttributeType = new Dictionary<Type, NBGPropertyDrawer>();
            metaByAttributeType[typeof(InfoBoxAttribute)] = new InfoBoxPropertyDrawer();
            //drawersByAttributeType[typeof(DisableIfAttribute)] = new DisableIfPropertyDrawer();
            //drawersByAttributeType[typeof(DropdownAttribute)] = new DropdownPropertyDrawer();
            //drawersByAttributeType[typeof(EnableIfAttribute)] = new EnableIfPropertyDrawer();
            //drawersByAttributeType[typeof(ProgressBarAttribute)] = new ProgressBarPropertyDrawer();
            //drawersByAttributeType[typeof(ReorderableListAttribute)] = new ReorderableListPropertyDrawer();
            //drawersByAttributeType[typeof(ResizableTextAreaAttribute)] = new ResizableTextAreaPropertyDrawer();
            //drawersByAttributeType[typeof(ShowAssetPreviewAttribute)] = new ShowAssetPreviewPropertyDrawer();
            //drawersByAttributeType[typeof(TagAttribute)] = new TagPropertyDrawer();
        }

        public static NBGPropertyDrawer GetMetaForAttribute(Type attributeType)
        {
            if (metaByAttributeType.TryGetValue(attributeType, out var drawer))
                return drawer;
            else
                return null;
        }

        public static NBGPropertyDrawer GetDrawerForAttribute(Type attributeType)
        {
            NBGPropertyDrawer drawer;
            if (drawersByAttributeType.TryGetValue(attributeType, out drawer))
                return drawer;
            else
                return null;
        }

        public static void ClearCache()
        {
        }
    }
}
