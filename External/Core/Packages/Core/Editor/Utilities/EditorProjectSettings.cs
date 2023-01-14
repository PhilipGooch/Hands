#if NBG_ENABLE_EDITOR_PROJECT_SETTINGS //TODO: determine the future of this unused feature
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NBG.Core
{
    /// <summary>
    /// Global project-specific settings.
    /// </summary>
    public static class EditorProjectSettings
    {
        [Serializable]
        class Entry
        {
            public string key;
            public string value;
        }

        [Serializable]
        class Data
        {
            public List<Entry> strings;
        }

        public const string SettingsPath = "ProjectSettings/NBG.Core.EditorProjectSettings.json";

        [ClearOnReload(newInstance: true)]
        static Dictionary<string, string> _values = new Dictionary<string, string>();

        [ClearOnReload]
        static bool _loaded;
        //[ClearOnReload]
        //static bool _dirty;

        static void EnsureLoaded()
        {
            if (_loaded)
                return;
            _loaded = true;

            try
            {
                var contents = File.ReadAllText(SettingsPath);
                var data = JsonUtility.FromJson<Data>(contents);
                foreach (var e in data.strings)
                    _values.Add(e.key, e.value);
            }
            catch
            {
                Save();
            }
        }

        static void Save()
        {
            //_dirty = false;
            try
            {
                var data = new Data();
                data.strings = new List<Entry>();
                foreach (var pair in _values)
                    data.strings.Add(new Entry { key = pair.Key, value = pair.Value });

                var contents = JsonUtility.ToJson(data, true);
                File.WriteAllText(SettingsPath, contents);
            }
            catch
            {
                Debug.LogError($"Failed to write {SettingsPath}");
            }
        }

        public static bool HasString(string key)
        {
            EnsureLoaded();
            return _values.ContainsKey(key);
        }

        public static string GetString(string key)
        {
            EnsureLoaded();
            return _values[key];
        }

        public static string GetString(string key, string fallbackValue)
        {
            EnsureLoaded();
            var ret = fallbackValue;
            _values.TryGetValue(key, out ret);
            return ret;
        }

        public static void SetString(string key, string value)
        {
            EnsureLoaded();
            if (_values.ContainsKey(key))
                _values[key] = value;
            else
                _values.Add(key, value);
            Save();
        }
    }
}
#endif //NBG_ENABLE_EDITOR_PROJECT_SETTINGS