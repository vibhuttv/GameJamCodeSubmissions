using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CursorSamurai.Core
{
    // Custom OS cursor (ink-brush tip / glowing scrub mode) + trail + click pulse.
    // Cursor is the character of this game — it must look alive.
    public class CursorFX : MonoBehaviour
    {
        Texture2D _default, _scrub;
        Vector2 _hotspotDefault = new Vector2(4, 4);
        Vector2 _hotspotScrub   = new Vector2(8, 8);
        bool _scrubActive;

        // UI-space trail + click pulse rendered via a screen-space overlay canvas
        Canvas _canvas;
        RectTransform _canvasRT;
        Sprite _trailSprite, _pulseSprite;

        readonly List<TrailDot> _dots = new List<TrailDot>();
        float _dotCooldown;

        struct TrailDot { public RectTransform rt; public Image img; public float t; public float life; public Vector2 pos; }

        public void Init(Canvas hudCanvas)
        {
            _canvas = hudCanvas;
            _canvasRT = _canvas.GetComponent<RectTransform>();

            _default = LoadTexture("Sprites/cursor_default");
            _scrub   = LoadTexture("Sprites/cursor_scrub");
            _trailSprite = Resources.Load<Sprite>("Sprites/cursor_trail");
            _pulseSprite = Resources.Load<Sprite>("Sprites/click_pulse");

            if (_default != null) Cursor.SetCursor(_default, _hotspotDefault, CursorMode.Auto);
        }

        static Texture2D LoadTexture(string resPath)
        {
            var s = Resources.Load<Sprite>(resPath);
            if (s == null) return null;
            // Convert sprite to Texture2D
            return s.texture;
        }

        public void SetScrubActive(bool active)
        {
            if (active == _scrubActive) return;
            _scrubActive = active;
            if (active && _scrub != null)     Cursor.SetCursor(_scrub,   _hotspotScrub,   CursorMode.Auto);
            else if (_default != null)         Cursor.SetCursor(_default, _hotspotDefault, CursorMode.Auto);
        }

        public void PulseClick(Vector2 screenPos)
        {
            if (_pulseSprite == null || _canvas == null) return;
            var go = new GameObject("ClickPulse");
            go.transform.SetParent(_canvas.transform, false);
            var img = go.AddComponent<Image>();
            img.sprite = _pulseSprite;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(64, 64);
            rt.anchoredPosition = ScreenToCanvas(screenPos);
            var pulse = go.AddComponent<ClickPulseAnim>();
            pulse.Image = img; pulse.Rect = rt;
        }

        public void Tick(Vector2 mouseScreenPx)
        {
            // Spawn trail dots every 15ms
            _dotCooldown -= Time.unscaledDeltaTime;
            if (_dotCooldown <= 0f && _trailSprite != null && _canvas != null) {
                var go = new GameObject("TrailDot");
                go.transform.SetParent(_canvas.transform, false);
                var img = go.AddComponent<Image>();
                img.sprite = _trailSprite;
                img.raycastTarget = false;
                img.color = _scrubActive ? new Color(0.5f, 0.8f, 1f, 0.9f) : new Color(1f, 0.87f, 0.47f, 0.85f);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(16, 16);
                rt.anchoredPosition = ScreenToCanvas(mouseScreenPx);
                _dots.Add(new TrailDot { rt = rt, img = img, t = 0, life = 0.35f, pos = mouseScreenPx });
                _dotCooldown = 0.015f;
            }

            // Fade + drift existing dots
            for (int i = _dots.Count - 1; i >= 0; i--) {
                var d = _dots[i];
                d.t += Time.unscaledDeltaTime;
                float k = d.t / d.life;
                if (k >= 1f) { Destroy(d.rt.gameObject); _dots.RemoveAt(i); continue; }
                var c = d.img.color; c.a = 0.85f * (1f - k); d.img.color = c;
                d.rt.sizeDelta = Vector2.one * Mathf.Lerp(16, 4, k);
                _dots[i] = d;
            }
        }

        Vector2 ScreenToCanvas(Vector2 px)
        {
            float scale = _canvas.scaleFactor > 0 ? _canvas.scaleFactor : 1f;
            return new Vector2(px.x / scale, px.y / scale);
        }
    }

    class ClickPulseAnim : MonoBehaviour
    {
        public Image Image;
        public RectTransform Rect;
        float _t;
        const float Life = 0.35f;
        void Update()
        {
            _t += Time.unscaledDeltaTime;
            float k = _t / Life;
            if (k >= 1f) { Destroy(gameObject); return; }
            float size = Mathf.Lerp(24, 96, k);
            Rect.sizeDelta = Vector2.one * size;
            var c = Image.color; c.a = 1f - k; Image.color = c;
        }
    }
}
