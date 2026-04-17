using System;
using UnityEngine;
using UnityEngine.UI;

namespace CursorSamurai.UI
{
    // Runtime title screen — no scene asset needed. Big title, pulsing "CLICK TO
    // START", mini animated samurai silhouette. Shown once on app launch.
    public class TitleScreen : MonoBehaviour
    {
        GameObject _root;
        Text _subtitle;
        float _pulseT;
        public bool Visible { get; private set; }
        Action _onStart;

        public void Init(Canvas canvas, Font font, Action onStart)
        {
            _onStart = onStart;

            _root = new GameObject("TitleScreen");
            _root.transform.SetParent(canvas.transform, false);
            var rt = _root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Dark gradient background
            var bg = _root.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.1f, 1f);
            bg.raycastTarget = true;
            var btn = _root.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => {
                Hide();
                _onStart?.Invoke();
            });

            // Animated samurai — run cycle playing in place on the left
            var samuraiGo = new GameObject("TitleSamurai");
            samuraiGo.transform.SetParent(_root.transform, false);
            var sImg = samuraiGo.AddComponent<Image>();
            sImg.raycastTarget = false;
            var sRT = samuraiGo.GetComponent<RectTransform>();
            sRT.anchorMin = sRT.anchorMax = new Vector2(0.22f, 0.48f);
            sRT.pivot = new Vector2(0.5f, 0.5f);
            sRT.sizeDelta = new Vector2(280, 320);
            var anim = samuraiGo.AddComponent<TitleSamuraiAnim>();
            anim.Img = sImg;
            anim.Frames = CursorSamurai.Core.SpriteCache.GetFrames("Sprites/Samurai", "run");

            // Title
            var title = MakeText(font, "CURSOR KNIGHT", 128, new Color(1f, 0.87f, 0.42f));
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.6f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(1400, 160);

            // Tag line
            var tag = MakeText(font, "a POINTER × RUNTIME runner", 28, new Color(0.74f, 0.68f, 0.5f));
            var tgRT = tag.GetComponent<RectTransform>();
            tgRT.anchorMin = tgRT.anchorMax = new Vector2(0.5f, 0.5f);
            tgRT.pivot = new Vector2(0.5f, 0.5f);
            tgRT.sizeDelta = new Vector2(900, 40);

            // Subtitle — pulsing
            _subtitle = MakeText(font, "click anywhere to begin", 36, new Color(0.96f, 0.92f, 0.78f));
            var srt = _subtitle.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.32f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(900, 80);

            // Controls hint
            var hint = MakeText(font, "Mouse = steer   Left-click = jump (double for air)   Right-click = slide   Hold Z + mouse-X = rewind", 18, new Color(0.5f, 0.48f, 0.42f));
            var hrt = hint.GetComponent<RectTransform>();
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.15f);
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.sizeDelta = new Vector2(1600, 30);

            // Credit
            var cred = MakeText(font, "MIT art: anshumanpattnaik forest-assassin · striderzz 2D-Platformer · opengameart", 14, new Color(0.4f, 0.38f, 0.35f));
            var crt = cred.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.06f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(1600, 20);

            Visible = true;
        }

        Text MakeText(Font font, string s, int size, Color col)
        {
            var go = new GameObject("t");
            go.transform.SetParent(_root.transform, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = s;
            t.fontSize = size;
            t.color = col;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.6f);
            sh.effectDistance = new Vector2(3, -3);
            return t;
        }

        public void Hide() { if (_root != null) _root.SetActive(false); Visible = false; }
        public void Show() { if (_root != null) _root.SetActive(true); Visible = true; }

        void Update()
        {
            if (!Visible || _subtitle == null) return;
            _pulseT += Time.unscaledDeltaTime;
            float a = 0.55f + Mathf.Abs(Mathf.Sin(_pulseT * 2.3f)) * 0.45f;
            var c = _subtitle.color; c.a = a; _subtitle.color = c;
        }
    }

    // Runs the title-screen samurai animation at a fixed cadence via UI Image.
    class TitleSamuraiAnim : MonoBehaviour
    {
        public UnityEngine.UI.Image Img;
        public Sprite[] Frames;
        float _t; int _idx;
        void Update()
        {
            if (Frames == null || Frames.Length == 0 || Img == null) return;
            _t += Time.unscaledDeltaTime * 12f;
            if (_t >= 1f) { _t -= 1f; _idx = (_idx + 1) % Frames.Length; Img.sprite = Frames[_idx]; }
            // Gentle bob
            var rt = (RectTransform)transform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, Mathf.Sin(Time.unscaledTime * 3f) * 4f);
        }
    }
}
