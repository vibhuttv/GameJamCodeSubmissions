using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CursorSamurai.UI
{
    // Minimal edit-mode Game view filler — just one samurai doing a slow bow/dance,
    // a title, and a subtitle. No BG stack, no ground, no coin. Clean composition.
    [ExecuteAlways]
    public class EditShowcase : MonoBehaviour
    {
        GameObject _samurai, _title, _subtitle;
        SpriteRenderer _sr;
        Sprite[] _frames;
        float _animT;
        int _frameIdx;

        void OnEnable()
        {
            if (Application.isPlaying) { CleanUp(); return; }
            Build();
#if UNITY_EDITOR
            EditorApplication.update += OnEditorUpdate;
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= OnEditorUpdate;
#endif
            CleanUp();
        }

        void CleanUp()
        {
            if (_samurai != null) DestroySafe(_samurai);
            if (_title    != null) DestroySafe(_title);
            if (_subtitle != null) DestroySafe(_subtitle);
            _samurai = _title = _subtitle = null;
        }

        void DestroySafe(Object o) {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        void Build()
        {
            // Single samurai sprite, centered, idle bow cycle (cheer frames ~4 fps)
            _frames = LoadFrames("cheer");
            if (_frames == null || _frames.Length == 0) _frames = LoadFrames("run");

            _samurai = new GameObject("Showcase_Samurai");
            _samurai.hideFlags = HideFlags.HideAndDontSave;
            _samurai.transform.SetParent(transform, false);
            _samurai.transform.localScale = Vector3.one * 0.3f;
            _samurai.transform.position = new Vector3(0f, -1f, 0f);
            _sr = _samurai.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 50;
            if (_frames != null && _frames.Length > 0) _sr.sprite = _frames[0];

            // Title
            _title = new GameObject("Showcase_Title");
            _title.hideFlags = HideFlags.HideAndDontSave;
            _title.transform.SetParent(transform, false);
            _title.transform.position = new Vector3(0f, 2.8f, 0f);
            var tm = _title.AddComponent<TextMesh>();
            tm.text = "CURSOR  KNIGHT";
            tm.fontSize = 80;
            tm.characterSize = 0.12f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.87f, 0.42f);
            _title.GetComponent<MeshRenderer>().sortingOrder = 120;

            _subtitle = new GameObject("Showcase_Subtitle");
            _subtitle.hideFlags = HideFlags.HideAndDontSave;
            _subtitle.transform.SetParent(transform, false);
            _subtitle.transform.position = new Vector3(0f, 1.6f, 0f);
            var sm = _subtitle.AddComponent<TextMesh>();
            sm.text = "press PLAY to begin";
            sm.fontSize = 40;
            sm.characterSize = 0.06f;
            sm.anchor = TextAnchor.MiddleCenter;
            sm.alignment = TextAlignment.Center;
            sm.color = new Color(0.92f, 0.88f, 0.74f);
            _subtitle.GetComponent<MeshRenderer>().sortingOrder = 120;
        }

        Sprite[] LoadFrames(string prefix)
        {
            var all = Resources.LoadAll<Sprite>("Sprites/Samurai");
            var list = new List<Sprite>();
            foreach (var s in all) if (s.name.StartsWith(prefix + "_")) list.Add(s);
            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            return list.ToArray();
        }

#if UNITY_EDITOR
        void OnEditorUpdate()
        {
            if (Application.isPlaying) { CleanUp(); return; }
            if (_samurai == null) Build();
            Tick(1f / 60f);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
#endif

        void Update()
        {
            if (Application.isPlaying) { if (_samurai != null) CleanUp(); return; }
            Tick(Time.deltaTime > 0 ? Time.deltaTime : 1f / 60f);
        }

        void Tick(float dt)
        {
            if (_samurai == null || _frames == null || _frames.Length == 0) return;
            // Slow frame cycle (bow / dance)
            _animT += dt * 3f;
            while (_animT >= 1f) { _animT -= 1f; _frameIdx = (_frameIdx + 1) % _frames.Length; }
            _sr.sprite = _frames[_frameIdx];
            // Gentle breathing bob
            float yBob = Mathf.Sin(Time.realtimeSinceStartup * 1.2f) * 0.08f;
            _samurai.transform.position = new Vector3(0f, -1f + yBob, 0f);
        }
    }
}
