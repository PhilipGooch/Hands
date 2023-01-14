namespace Automation.Core
{
    public enum Severity
    {
        Info,
        Error,
        Warning,
        WeakWarning
    }

    public struct InspectionElement
    {
        public string id;
        public string file;
        public int line;
        public string message;
        public Severity severity;
    }

    public interface ILog
    {
        void Message(string text);

        void Message(string text, string flowId);
        void Warning(string text, string flowId = null);
        void Error(string text, string flowId = null);

        void Status(string text);

        void PushBlock(string name, string description = null);
        void PopBlock();

        void InspectionType(string id, string name, string category, string description = "No description");
        void Inspection(InspectionElement inspectionElement);

        void PublishArtifacts(string source, string destination = null);
    }
}
