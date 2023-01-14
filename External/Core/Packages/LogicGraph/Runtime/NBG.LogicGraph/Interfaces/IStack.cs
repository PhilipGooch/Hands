namespace NBG.LogicGraph
{
    /// <summary>
    /// LogicGraph variable stack.
    /// </summary>
    public interface IStack
    {
        void Pop();

        void PushBool(bool value);
        bool PopBool();

        void PushInt(int value);
        int PopInt();

        void PushFloat(float value);
        float PopFloat();

        void PushString(string value);
        string PopString();

        void PushVector3(UnityEngine.Vector3 value);
        UnityEngine.Vector3 PopVector3();

        void PushObject(UnityEngine.Object value);
        UnityEngine.Object PopObject();

        void PushQuaternion(UnityEngine.Quaternion value);
        UnityEngine.Quaternion PopQuaternion();

        void PushColor(UnityEngine.Color value);
        UnityEngine.Color PopColor();
    }
}
