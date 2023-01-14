using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Automation.Core
{
    public class InspectionRule
    {
        public string Rule { get; set; }
        public string Category { get; set; }
        public Severity Severity { get; set; }
    }

    public static class Inspection
    {
        public static int ParseLogFile(string path, string root, string rulesFilePath)
        {
            try
            {
                // Default
                {
                    var defaultRulesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inspection.rules.default.json");
                    Utils.Log.Message($"Inspecting using default rules from {defaultRulesFilePath}");
                    var rules = ParseInspectionRules(defaultRulesFilePath);
                    ParseLogFile_Internal(path, root, rules);
                }

                // User specified
                if (!string.IsNullOrWhiteSpace(rulesFilePath))
                {
                    Utils.Log.Message($"Inspecting using additional rules from {rulesFilePath}");
                    var rules = ParseInspectionRules(rulesFilePath);
                    ParseLogFile_Internal(path, root, rules);
                }
                
                return 0;

            }
            catch (Exception e)
            {
                Utils.Log.Warning($"Failed to parse Unity log file: {e.Message} \n {e.StackTrace}");
                return -1;
            }
        }

        static void ParseLogFile_Internal(string path, string root, InspectionRule[] rules)
        {
            string logString = File.ReadAllText(path);

            foreach (var rule in rules)
            {
                SearchErrorPattern(logString, root, rule.Rule, rule.Category, rule.Severity);
            }
        }

        static void SearchErrorPattern(string logString, string root, string regexPattern, string category, Severity severity = Severity.Warning)
        {
            root = FixDirectorySeparators(root);

            Regex regex = new Regex(regexPattern, RegexOptions.Compiled);
            var matches = regex.Matches(logString);

            Dictionary<string, string> warningTypes = new Dictionary<string, string>();
            foreach (Match match in matches)
            {
                string warningType = match.Groups["warningType"].Value;
                if (!warningTypes.ContainsKey(warningType))
                    warningTypes.Add(warningType, match.Groups["warningDescription"].Value);
            }

            foreach (var pair in warningTypes)
            {
                Utils.Log.InspectionType(pair.Key, pair.Key, category, pair.Value);
            }


            HashSet<InspectionElement> inspections = new HashSet<InspectionElement>();
            foreach (Match match in matches)
            {
                GroupCollection groups = match.Groups;

                string message = "";
                foreach (var part in groups["message"].Captures)
                    message += part;

                string file = groups["file"].Value;
                file = FixDirectorySeparators(file);
                file = RemovePathRoot(file, root);

                inspections.Add(new InspectionElement
                {
                    id = groups["warningType"].Value,
                    file = file,
                    line = int.Parse(groups["line"].Value),
                    message = message,
                    severity = severity
                }
                );
            }

            foreach (var inpection in inspections)
                Utils.Log.Inspection(inpection);
        }

        static string FixDirectorySeparators(string path)
        {
            return path.Replace(@"\", "/");
        }

        static string RemovePathRoot(string path, string root)
        {
            try
            {
                if (!string.IsNullOrEmpty(root))
                {
                    if (path.StartsWith(root))
                        path = path.Substring(Math.Min(root.Length + 1, path.Length));
                }
            }
            catch
            {
            }

            return path;
        }

        public static InspectionRule[] ParseInspectionRules(string path)
        {
            try
            {
                Utils.Log.Message($"Parsing rules from file: {path}");
                var opts = new JsonSerializerOptions();
                opts.IncludeFields = true;
                opts.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                var rules = JsonSerializer.Deserialize<InspectionRule[]>(File.ReadAllText(path), opts);

                Utils.Log.Message($"Using {rules.Length} rules:");
                foreach (var rule in rules)
                {
                    Utils.Log.Message($"[{rule.Category}][{rule.Severity}] {rule.Rule}");
                }

                return rules;
            }
            catch (Exception e)
            {
                Utils.Log.Message($"Failed to parse inspection rules file: {e.Message}");
                return null;
            }
        }
    }
}
