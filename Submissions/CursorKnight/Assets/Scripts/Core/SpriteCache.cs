using System.Collections.Generic;
using UnityEngine;

namespace CursorSamurai.Core
{
    // Centralized sprite loader. Caches Resources.Load results so level switches don't
    // re-pay the load cost.
    public static class SpriteCache
    {
        static readonly Dictionary<string, Sprite>   _single = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite[]> _folder = new Dictionary<string, Sprite[]>();

        public static Sprite Get(string path)
        {
            if (_single.TryGetValue(path, out var s) && s != null) return s;
            s = Resources.Load<Sprite>(path);
            if (s != null) _single[path] = s;
            return s;
        }

        // All sprites in a folder with a given filename prefix (e.g. "run").
        public static Sprite[] GetFrames(string folderPath, string prefix)
        {
            string key = folderPath + "/" + prefix;
            if (_folder.TryGetValue(key, out var arr) && arr != null && arr.Length > 0) return arr;
            var all = Resources.LoadAll<Sprite>(folderPath);
            var list = new List<Sprite>();
            foreach (var s in all) if (s.name.StartsWith(prefix + "_")) list.Add(s);
            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            arr = list.ToArray();
            _folder[key] = arr;
            return arr;
        }
    }
}
