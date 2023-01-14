using System;

namespace NBG.NodeGraph
{
    public class NBGAttribute : Attribute
    {
        public enum ConditionOperator
        {
            And,
            Or
        }
    }
    public class DrawerAttribute : NBGAttribute
    {

    }

    public class MetaAttribute : NBGAttribute
    {

    }

    public class GroupAttribute : NBGAttribute
    {
        public string Name { get; private set; }

        public GroupAttribute(string name)
        {
            this.Name = name;
        }
    }

    public class ReadOnlyAttribute : DrawerAttribute
    {
    }
    public class RenameAttribute : DrawerAttribute
    {
        public string name;
        public RenameAttribute(string newName)
        {
            name = newName;
        }
    }

    public abstract class ValidatorAttribute : NBGAttribute
    {

    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class RequiredAttribute : ValidatorAttribute
    {
        public string Message { get; private set; }

        public RequiredAttribute(string message = null)
        {
            this.Message = message;
        }
    }

    public class InfoBoxAttribute : MetaAttribute
    {
        public string text;
        public InfoBoxType type;

        public InfoBoxAttribute(string text, InfoBoxType type = InfoBoxType.info)
        {
            this.text = text;
            this.type = type;
        }

        public enum InfoBoxType
        {
            info = 1,
            Warning = 2,
            Error = 3
        }
    }
}
