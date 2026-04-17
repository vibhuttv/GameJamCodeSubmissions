using UnityEngine;
using CursorSamurai.Core;

namespace CursorSamurai.Entities
{
    // Renamed internally to Coin — cycles through 16-frame sprite animation.
    // Kept as `Crystal` class for back-compat with existing RunnerLevel code.
    public class Crystal : MonoBehaviour
    {
        public bool Collected;
        static Sprite[] _frames;
        SpriteRenderer _sr;
        float _frameT;
        int _frameIdx;
        const float FrameFps = 22f;   // faster spin so the edge-on frames blur naturally

        public static Crystal Build()
        {
            var go = new GameObject("Coin");
            var cr = go.AddComponent<Crystal>();
            cr._sr = go.AddComponent<SpriteRenderer>();
            cr._sr.sortingOrder = 40;
            if (_frames == null || _frames.Length == 0)
                _frames = SpriteCache.GetFrames("Sprites/Coin", "coin");
            if (_frames != null && _frames.Length > 0)
                cr._sr.sprite = _frames[0];
            go.transform.localScale = Vector3.one * 0.4f;   // coin sprites are ~300px → scale down
            return cr;
        }

        void Update()
        {
            if (Collected) return;
            // Animate spin by cycling frames
            if (_frames != null && _frames.Length > 0) {
                _frameT += Time.deltaTime * FrameFps;
                while (_frameT >= 1f) {
                    _frameT -= 1f;
                    _frameIdx = (_frameIdx + 1) % _frames.Length;
                    _sr.sprite = _frames[_frameIdx];
                }
            }
        }

        public void SetBasePos(Vector3 p) { transform.position = p; }

        public Bounds GetHitbox() => new Bounds(transform.position, new Vector3(0.5f, 0.5f, 1f));
    }
}
