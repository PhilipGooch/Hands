using NBG.LogicGraph.EditorInterface;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.LogicGraph.EditorUI
{
    public static class Parameters
    {
        public static Color defaultGroupColor = new Color32(75, 69, 69, 255);

        public static Color nodeFlavorTextColor = new Color32(145, 145, 145, 255);
        public static Color nodeSelectionBorderColor = new Color32(3, 152, 252, 255);

        public static Color fieldSelectionColor = new Color32(252, 186, 3, 40);

        //Comments
        public static Color commentBackgroundColor = new Color32(252, 215, 110, 255);
        public static Color commentTextColor = new Color32(88, 67, 8, 255);
        public static Color commentTextHoverColor = new Color32(3, 152, 252, 255);

        //Ports
        public static Color unknownPortColor = new Color32(132, 140, 184, 255);
        public static Color flowDividerColor => flowPortColor;

        private static Color flowPortColor = Color.white;
        private static Color intPortColor = new Color32(34, 230, 174, 255);
        private static Color floatPortColor = new Color32(154, 253, 66, 255);
        private static Color stringPortColor = new Color32(250, 1, 212, 255);
        private static Color objectPortColor = new Color32(2, 165, 237, 255);
        private static Color boolPortColor = new Color32(128, 0, 0, 255);
        private static Color vectorPortColor = new Color32(247, 195, 31, 255);
        private static Color quaternionPortColor = new Color32(157, 177, 255, 255);
        private static Color colorPortColor = new Color32(100, 137, 237, 255);

        public static Color activatedNodeColor = new Color32(176, 68, 0, 255);

        public static Color darkSkinTabBackgroundColor = new Color32(56, 56, 56, 255);
        public static Color lightSkinTabBackgroundColor = new Color32(200, 200, 200, 255);

        public static readonly Dictionary<Type, Color> PortColors = new Dictionary<Type, Color>()
        {
            {typeof(string),stringPortColor },
            {typeof(int),intPortColor },
            {typeof(float),floatPortColor },
            {typeof(object),objectPortColor },
            {typeof(bool),boolPortColor },
            {typeof(Vector3),vectorPortColor },
            {typeof(Quaternion),quaternionPortColor },
            {typeof(INode),flowPortColor },
            {typeof(Color),colorPortColor },
        };

        public static readonly Dictionary<NodeConceptualType, Color> NodeColors = new Dictionary<NodeConceptualType, Color>()
        {
            {NodeConceptualType.EntryPoint,new Color32(77, 36, 35, 205) },
            {NodeConceptualType.Function,new Color32(36, 50, 77, 205) },
            {NodeConceptualType.FlowControl,new Color32(63, 63, 63, 205) },
            {NodeConceptualType.TypeConverter,new Color32(1, 121, 130, 205) },
            {NodeConceptualType.Getter,new Color32(54, 77, 58, 205) },
            {NodeConceptualType.Undefined,new Color32(20, 20, 20, 205) }
        };

        public static readonly Dictionary<NodeConceptualType, Color> HighContrastNodeColors = new Dictionary<NodeConceptualType, Color>()
        {
            {NodeConceptualType.EntryPoint,new Color32(153, 47, 44, 255) },
            {NodeConceptualType.Function,new Color32(43, 75, 135, 255) },
            {NodeConceptualType.FlowControl,new Color32(122, 121, 121, 255) },
            {NodeConceptualType.TypeConverter,new Color32(0, 187, 201, 255) },
            {NodeConceptualType.Getter,new Color32(29, 125, 45, 255) },
            {NodeConceptualType.Undefined,new Color32(10, 10, 10, 255) }
        };

        public static readonly Dictionary<Severity, Color> ToastNotificationColors = new Dictionary<Severity, Color>()
        {
            {Severity.Info,new Color32(33, 151, 241, 255) },
            {Severity.Warning,new Color32(255, 151, 2, 255) },
            {Severity.Error,new Color32(185, 9, 9, 255) },
        };

        public static Texture2D functionIcon = Resources.Load<Texture2D>("function");

        //searcher
        public static Color seacherSelectableHoverOnColor = new Color32(76, 141, 194, 63);
        public static Color seacherSelectableSelectedColor = new Color(1, 1, 1, 0.15f);

        public const float foldoutLayerWidth = 15.2f;
    }
}
