using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Automation.Core
{
    // TeamCity specific logger. Writes to stdout and formats service messages.
    public class LogTeamCity : ILog
    {
        Stack<string> _blocks = new Stack<string>();

        public static string Encode(string value)
        {
            if (value == null)
                return null;
            return JetBrains.TeamCity.ServiceMessages.ServiceMessageReplacements.Encode(value);
        }

        public void Message(string text)
        {
            Console.WriteLine(text);
        }

        public void Message(string text, string flowId)
        {
            text = Encode(text);
            flowId = Encode(flowId);
            Console.WriteLine($"##teamcity[message text='{text}' flowId='{flowId}']");
        }
        public void Warning(string text, string flowId)
        {
            text = Encode(text);
            if (flowId != null)
            {
                flowId = Encode(flowId);
                Console.WriteLine($"##teamcity[message text='{text}' flowId='{flowId}' status='WARNING']");
            }
            else
                Console.WriteLine($"##teamcity[message text='{text}' status='WARNING']");
        }

        public void Error(string text, string flowId)
        {
            text = Encode(text);
            if (flowId != null)
            {
                flowId = Encode(flowId);
                Console.WriteLine($"##teamcity[message text='{text}' flowId='{flowId}' status='ERROR']");
            }
            else
                Console.WriteLine($"##teamcity[message text='{text}' status='ERROR']");
        }

        public void Status(string text)
        {
            text = Encode(text);
            Console.WriteLine($"##teamcity[buildStatus text='{text}']");
        }

        public void PushBlock(string name, string description = null)
        {
            name = Encode(name);
            description = Encode(description);

            _blocks.Push(name);
            Console.WriteLine($"##teamcity[blockOpened name='{name}' description='{description}']");
        }

        public void PopBlock()
        {
            if (_blocks.Count == 0)
                throw new InvalidOperationException();
            var name = _blocks.Pop();
            Console.WriteLine($"##teamcity[blockClosed name='{name}']");
        }

        public void InspectionType(string id, string name, string category, string description)
        {
            //https://www.jetbrains.com/help/teamcity/service-messages.html#Inspection+type

            id = Encode(id);
            name = Encode(name);
            category = Encode(category);
            description = Encode(description);

            Console.WriteLine($"##teamcity[inspectionType id='{id}' name='{name}' description='{description}' category='{category}']");
        }

        public void Inspection(InspectionElement inspectionElement)
        {
            //https://www.jetbrains.com/help/teamcity/service-messages.html#Inspection+instance

            string id = Encode(inspectionElement.id);
            string message = Encode(inspectionElement.message);
            string file = Encode(inspectionElement.file);

            string severityValue = "";

            severityValue = inspectionElement.severity switch
            {
                Severity.Info => "INFO",
                Severity.Error => "ERROR",
                Severity.Warning => "WARNING",
                Severity.WeakWarning => "WEAK WARNING",
                _ => ""
            };

            Console.WriteLine($"##teamcity[inspection typeId='{id}' message='{message}' file='{file}' line='{inspectionElement.line}' SEVERITY='{severityValue}']");
        }

        public void PublishArtifacts(string source, string destination)
        {
            Debug.Assert(source != null);

            source = Encode(source);
            destination = Encode(destination);

            if (destination == null)
                Console.WriteLine($"##teamcity[publishArtifacts '{source}']");
            else
                Console.WriteLine($"##teamcity[publishArtifacts '{source}=>{destination}']");
        }
    }
}
