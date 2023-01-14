using System;
using System.IO;
using UnityEngine;

namespace NBG.Core
{
    [Serializable]
    public class BuildVersion
    {
        const string kFileName = "Version.json";

        [ClearOnReload]
        private static BuildVersion instance = null;
        public static BuildVersion Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BuildVersion();

                    try
                    {
                        var data = Read();
                        if (data == null)
                            throw new InvalidDataException();
                        instance.data = data;
                    }
                    catch
                    {
                        Debug.Log($"Failed to read Version.json. Initializing defaults.");
                    }
                }
                return instance;
            }
        }

        BuildVersionData data = new BuildVersionData();

        public string Branch => data.branch;
        public string Hash => data.hash;
        public int BuildNumber => data.buildNumber;
        public string VersionName => data.versionName;

        static BuildVersionData Read()
        {
            var filePath = Path.Combine(Application.streamingAssetsPath, kFileName);
            var data = JsonUtility.FromJson<BuildVersionData>(File.ReadAllText(filePath));
            return data;
        }

        static void Write()
        {
            try
            {
                var filePath = Path.Combine(Application.streamingAssetsPath, kFileName);
                File.WriteAllText(filePath, JsonUtility.ToJson(Instance.data, true));
            }
            catch
            {
                Debug.LogError($"Failed to write Version.json");
            }
        }

        public static void Write(string branch, string hash, int buildNumber, string versionName = "manual build from Editor")
        {
            var version = Instance.data;

            version.branch = branch;
            version.hash = hash;
            version.buildNumber = buildNumber;
            version.versionName = versionName;

            Write();
        }

        internal void Refresh()
        {
            try
            {
                var data = Read();
                if (data != null)
                    instance.data = data;
                else
                    instance.data = new BuildVersionData();
            }
            catch
            {
            }
        }

        public override string ToString()
        {
            return $"[BuildVersion] Branch: {Branch}; Hash: {Hash}; Number: {BuildNumber}; Version: {VersionName};";
        }
    }
}
