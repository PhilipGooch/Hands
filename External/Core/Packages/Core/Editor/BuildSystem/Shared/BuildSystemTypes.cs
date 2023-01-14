// This file is shared by NBG.Core Unity assembly and Automations Tools

using System;

namespace NBG.Core
{
    public enum BuildPlatform
    {
        Windows,
        MacOS,
        Linux,

        Android,

        Switch,
    }

    public enum BuildConfiguration
    {
        Development,
        Release,
    }

    public enum BuildScripting
    {
        Auto,
        Mono,
        IL2CPP,
    }

    [Flags]
    public enum EditorTestsPlatform
    {
        EditMode = (1 << 0),
        PlayMode = (1 << 1),
        All = EditMode | PlayMode,
    }

    public enum CacheServerConnectionState
    {
        Inherited,
        Enabled,
        Disabled,
    }

    public static class BuildSystemCommandLineArgs
    {
        public static string Platform = "--platform=";
        public static string Configuration = "--configuration=";
        public static string Scripting = "--scripting=";
    }
}
