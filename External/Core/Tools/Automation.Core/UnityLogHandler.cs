using System;
using System.IO;
using NiceIO;

namespace Automation.Core
{
    public class UnityLogHandler
    {
        ILog _log;
        NPath _filePath;
        StreamWriter _writer;

        const string kFlowId = "Unity";

        public UnityLogHandler(ILog log, string filePath)
        {
            _log = log;
            _filePath = new NPath(filePath);

            _log.Message($"Collecting Unity log file to {filePath}");
            _filePath.CreateFile();
            _writer = new StreamWriter(filePath);
        }

        public void OnStdOut(string text)
        {
            if (text.ToLowerInvariant().Contains("error"))
                _log.Error(text, kFlowId);
            else if (text.ToLowerInvariant().Contains("warning"))
                _log.Warning(text, kFlowId);
            else
                _log.Message(text, kFlowId);

            _writer.WriteLine(text);
        }

        public void OnStdErr(string text)
        {
            _log.Error(text, kFlowId);
            _writer.WriteLine(text);
        }

        public void Close()
        {
            _writer.Close();
            _writer.Dispose();
            _writer = null;

            _log.PublishArtifacts(_filePath, "Logs");
        }
    }
}
