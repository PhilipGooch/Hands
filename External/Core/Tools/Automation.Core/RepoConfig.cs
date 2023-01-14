using System.Collections.Generic;

namespace Automation
{
    public class RepoConfig
    {
        public string UnityProjectDir { get; set; }
        public string GameName { get; set; }
        public string GameVersion { get; set; }
        public string CoreModulePath { get; set; }
        public bool UnityLogInspection { get; set; }
        public string UnityLogInspectionRulesPath { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
    }
}
