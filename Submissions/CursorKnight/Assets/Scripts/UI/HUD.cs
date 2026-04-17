using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CursorSamurai.UI
{
    // Rewritten HUD — structured panels with icons, progress bars, level badge.
    // Designed for visual impact (bloom-friendly bright colors + dark panel frames).
    public class HUD : MonoBehaviour
    {
        Canvas _canvas;
        Font _font;

        // Left stats panel — coin row + score row + time row with progress bar
        Text _coinValue, _scoreValue, _timeValue;
        Image _timeFill;
        RectTransform _timePanelRT;
        RectTransform _statsPanelRT;

        // Right — level badge + combo
        Text _levelNumber, _comboTagline;
        RectTransform _badgeRT;

        // Legacy center banner + scrub bar (kept)
        Text _bottomText, _centerText;
        GameObject _overlay;
        Transform _overlayButtons;
        Text _overlayTitle, _overlayHint;
        GameObject _scrubBar;
        RectTransform _scrubPlayhead;
        Image _scrubPlayheadImg;
        Text _scrubLabel;
        float _centerUntil;

        // Popping combo text in middle of screen
        Text _comboText;
        int _lastCombo;

        // Punch-scale on coin pickup
        float _coinPunchT;

        public void Init()
        {
            if (FindAnyObjectByType<EventSystem>() == null) {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var cgo = new GameObject("HudCanvas");
            cgo.transform.SetParent(transform, false);
            _canvas = cgo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            cgo.AddComponent<GraphicRaycaster>();

            _font = Resources.Load<Font>("Fonts/PressStart2P")
                  ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildStatsPanel(cgo.transform);
            BuildLevelBadge(cgo.transform);
            BuildCenterBanner(cgo.transform);
            BuildBottomHint(cgo.transform);
            BuildScrubBar(cgo.transform);
            BuildOverlay(cgo.transform);
        }

        // ---------- BUILD METHODS ----------

        void BuildStatsPanel(Transform parent)
        {
            var panelGo = new GameObject("StatsPanel");
            panelGo.transform.SetParent(parent, false);
            _statsPanelRT = panelGo.AddComponent<RectTransform>();
            _statsPanelRT.anchorMin = new Vector2(0, 1);
            _statsPanelRT.anchorMax = new Vector2(0, 1);
            _statsPanelRT.pivot = new Vector2(0, 1);
            _statsPanelRT.anchoredPosition = new Vector2(24, -24);
            _statsPanelRT.sizeDelta = new Vector2(480, 220);

            var panelBg = panelGo.AddComponent<Image>();
            panelBg.sprite = Resources.Load<Sprite>("Sprites/UI/panel");
            panelBg.type = Image.Type.Sliced;
            panelBg.pixelsPerUnitMultiplier = 2;
            panelBg.color = new Color(1, 1, 1, 1);
            panelBg.raycastTarget = false;

            // Coin row
            var coinIcon = MakeIcon(panelGo.transform, "Sprites/UI/icon_coin", new Vector2(28, -28), 48);
            _coinValue = MakeText(panelGo.transform, "0 / 0", 30,
                                   new Color(1f, 0.86f, 0.36f),
                                   new Vector2(90, -34), TextAnchor.UpperLeft, 380, 40);

            // Score row
            MakeIcon(panelGo.transform, "Sprites/UI/icon_star", new Vector2(28, -88), 48);
            _scoreValue = MakeText(panelGo.transform, "0", 32,
                                    new Color(1f, 0.95f, 0.8f),
                                    new Vector2(90, -94), TextAnchor.UpperLeft, 380, 40);

            // Time row
            MakeIcon(panelGo.transform, "Sprites/UI/icon_clock", new Vector2(28, -148), 48);
            _timeValue = MakeText(panelGo.transform, "0.0s / 30s", 22,
                                   new Color(0.72f, 0.92f, 1f),
                                   new Vector2(90, -146), TextAnchor.UpperLeft, 380, 30);

            // Time progress bar (under the time row)
            var barBgGo = new GameObject("TimeBarBg");
            barBgGo.transform.SetParent(panelGo.transform, false);
            var barBg = barBgGo.AddComponent<Image>();
            barBg.sprite = Resources.Load<Sprite>("Sprites/UI/bar_bg");
            barBg.type = Image.Type.Sliced;
            barBg.pixelsPerUnitMultiplier = 2;
            barBg.raycastTarget = false;
            var bbRT = barBgGo.GetComponent<RectTransform>();
            bbRT.anchorMin = new Vector2(0, 1);
            bbRT.anchorMax = new Vector2(0, 1);
            bbRT.pivot = new Vector2(0, 1);
            bbRT.anchoredPosition = new Vector2(88, -178);
            bbRT.sizeDelta = new Vector2(360, 14);

            var barFillGo = new GameObject("TimeBarFill");
            barFillGo.transform.SetParent(barBgGo.transform, false);
            _timeFill = barFillGo.AddComponent<Image>();
            _timeFill.sprite = Resources.Load<Sprite>("Sprites/UI/bar_fill");
            _timeFill.type = Image.Type.Filled;
            _timeFill.fillMethod = Image.FillMethod.Horizontal;
            _timeFill.fillAmount = 1f;
            _timeFill.raycastTarget = false;
            var tfRT = barFillGo.GetComponent<RectTransform>();
            tfRT.anchorMin = Vector2.zero; tfRT.anchorMax = Vector2.one;
            tfRT.offsetMin = new Vector2(2, 2); tfRT.offsetMax = new Vector2(-2, -2);
            _timePanelRT = tfRT;
        }

        void BuildLevelBadge(Transform parent)
        {
            var root = new GameObject("LevelBadge");
            root.transform.SetParent(parent, false);
            _badgeRT = root.AddComponent<RectTransform>();
            _badgeRT.anchorMin = new Vector2(1, 1);
            _badgeRT.anchorMax = new Vector2(1, 1);
            _badgeRT.pivot = new Vector2(1, 1);
            _badgeRT.anchoredPosition = new Vector2(-30, -30);
            _badgeRT.sizeDelta = new Vector2(300, 180);

            // Hex shield
            var shield = new GameObject("Shield");
            shield.transform.SetParent(root.transform, false);
            var shieldImg = shield.AddComponent<Image>();
            shieldImg.sprite = Resources.Load<Sprite>("Sprites/UI/level_badge");
            shieldImg.raycastTarget = false;
            var shRT = shield.GetComponent<RectTransform>();
            shRT.anchorMin = new Vector2(1, 1);
            shRT.anchorMax = new Vector2(1, 1);
            shRT.pivot = new Vector2(1, 1);
            shRT.anchoredPosition = new Vector2(-6, -6);
            shRT.sizeDelta = new Vector2(140, 140);

            _levelNumber = MakeText(shield.transform, "1", 64,
                                    new Color(1f, 0.88f, 0.4f),
                                    new Vector2(0, 12), TextAnchor.MiddleCenter, 140, 80);
            var lvLbl = MakeText(shield.transform, "LV", 16,
                                 new Color(0.7f, 0.6f, 0.3f),
                                 new Vector2(0, -38), TextAnchor.MiddleCenter, 140, 20);

            // Combo badge below
            _comboTagline = MakeText(root.transform, "", 16,
                                     new Color(1f, 0.8f, 0.36f),
                                     new Vector2(-6, -150), TextAnchor.UpperRight, 300, 24);
            var cRT = _comboTagline.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(1, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(1, 1);
        }

        void BuildCenterBanner(Transform parent)
        {
            _centerText = MakeText(parent, "", 72, new Color(1f, 0.86f, 0.45f),
                                    new Vector2(0, 100), TextAnchor.MiddleCenter, 1200, 160);
            var rt = _centerText.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        void BuildBottomHint(Transform parent)
        {
            _bottomText = MakeText(parent, "", 14, new Color(0.7f, 0.66f, 0.55f, 0.9f),
                                    new Vector2(0, 36), TextAnchor.LowerCenter, 1600, 24);
            var rt = _bottomText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            _bottomText.text = "MOUSE = steer   LMB = jump (2x)   RMB = slide   HOLD Z + MOUSE X = rewind time";
        }

        void BuildScrubBar(Transform parent)
        {
            _scrubBar = new GameObject("ScrubBar");
            _scrubBar.transform.SetParent(parent, false);
            var barRT = _scrubBar.AddComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0.5f, 0f);
            barRT.anchorMax = new Vector2(0.5f, 0f);
            barRT.pivot = new Vector2(0.5f, 0f);
            barRT.anchoredPosition = new Vector2(0, 84);
            barRT.sizeDelta = new Vector2(520f, 30f);

            var bgImg = _scrubBar.AddComponent<Image>();
            bgImg.sprite = Resources.Load<Sprite>("Sprites/UI/panel");
            bgImg.type = Image.Type.Sliced;
            bgImg.pixelsPerUnitMultiplier = 2;
            bgImg.raycastTarget = false;

            // Tick marks
            for (int i = -4; i <= 3; i++) {
                var tick = new GameObject("T").AddComponent<Image>();
                tick.transform.SetParent(_scrubBar.transform, false);
                tick.color = new Color(0.4f, 0.5f, 0.6f, 0.5f);
                tick.raycastTarget = false;
                var tRT = tick.GetComponent<RectTransform>();
                tRT.anchorMin = new Vector2(0.5f, 0.5f);
                tRT.anchorMax = new Vector2(0.5f, 0.5f);
                tRT.pivot = new Vector2(0.5f, 0.5f);
                tRT.sizeDelta = new Vector2(1.5f, 10f);
                tRT.anchoredPosition = new Vector2(i * 60f, 0);
            }

            var now = new GameObject("Now").AddComponent<Image>();
            now.transform.SetParent(_scrubBar.transform, false);
            now.color = new Color(0.95f, 0.82f, 0.45f, 1f);
            now.raycastTarget = false;
            var nRT = now.GetComponent<RectTransform>();
            nRT.anchorMin = new Vector2(0.5f, 0.5f);
            nRT.anchorMax = new Vector2(0.5f, 0.5f);
            nRT.pivot = new Vector2(0.5f, 0.5f);
            nRT.sizeDelta = new Vector2(3f, 24f);

            var ph = new GameObject("Playhead");
            ph.transform.SetParent(_scrubBar.transform, false);
            _scrubPlayheadImg = ph.AddComponent<Image>();
            _scrubPlayheadImg.color = new Color(0.4f, 0.75f, 1f, 0.5f);
            _scrubPlayheadImg.raycastTarget = false;
            _scrubPlayhead = ph.GetComponent<RectTransform>();
            _scrubPlayhead.anchorMin = new Vector2(0.5f, 0.5f);
            _scrubPlayhead.anchorMax = new Vector2(0.5f, 0.5f);
            _scrubPlayhead.pivot = new Vector2(0.5f, 0.5f);
            _scrubPlayhead.sizeDelta = new Vector2(6f, 28f);

            _scrubLabel = MakeText(_scrubBar.transform, "RUNTIME", 10,
                                    new Color(0.9f, 0.92f, 1f, 0.9f),
                                    new Vector2(0, 2), TextAnchor.LowerCenter, 520, 18);
            var lRT = _scrubLabel.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0.5f, 1f);
            lRT.anchorMax = new Vector2(0.5f, 1f);
            lRT.pivot = new Vector2(0.5f, 0f);
        }

        void BuildOverlay(Transform parent)
        {
            _overlay = new GameObject("Overlay");
            _overlay.transform.SetParent(parent, false);
            var bg = _overlay.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.75f);
            var bgRT = _overlay.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(_overlay.transform, false);
            var pImg = panelGo.AddComponent<Image>();
            pImg.sprite = Resources.Load<Sprite>("Sprites/UI/panel");
            pImg.type = Image.Type.Sliced;
            pImg.pixelsPerUnitMultiplier = 2;
            var pRT = panelGo.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.5f, 0.5f);
            pRT.anchorMax = new Vector2(0.5f, 0.5f);
            pRT.pivot = new Vector2(0.5f, 0.5f);
            pRT.sizeDelta = new Vector2(880, 520);

            _overlayTitle = MakeText(panelGo.transform, "", 44,
                                      new Color(1f, 0.88f, 0.52f),
                                      new Vector2(0, -60), TextAnchor.UpperCenter, 820, 160);
            var tRT = _overlayTitle.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0.5f, 1);
            tRT.anchorMax = new Vector2(0.5f, 1);
            tRT.pivot = new Vector2(0.5f, 1);

            _overlayHint = MakeText(panelGo.transform, "", 12,
                                     new Color(0.72f, 0.66f, 0.55f),
                                     new Vector2(0, 30), TextAnchor.LowerCenter, 820, 50);
            var hRT = _overlayHint.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0.5f, 0);
            hRT.anchorMax = new Vector2(0.5f, 0);
            hRT.pivot = new Vector2(0.5f, 0);

            var btns = new GameObject("Btns");
            btns.transform.SetParent(panelGo.transform, false);
            var brt = btns.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(600, 240);
            brt.anchoredPosition = new Vector2(0, -40);
            var grp = btns.AddComponent<VerticalLayoutGroup>();
            grp.spacing = 12;
            grp.childAlignment = TextAnchor.MiddleCenter;
            grp.childControlWidth = true; grp.childControlHeight = true;
            grp.childForceExpandWidth = true; grp.childForceExpandHeight = false;
            _overlayButtons = btns.transform;

            _overlay.SetActive(false);
        }

        // ---------- HELPERS ----------

        Text MakeText(Transform parent, string s, int fs, Color col,
                      Vector2 anchoredPos, TextAnchor align, float w, float h)
        {
            var go = new GameObject("T");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.text = s;
            t.fontSize = fs;
            t.color = col;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.85f);
            sh.effectDistance = new Vector2(2, -2);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(w, h);
            return t;
        }

        Image MakeIcon(Transform parent, string resPath, Vector2 pos, float size)
        {
            var go = new GameObject("Icon");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = Resources.Load<Sprite>(resPath);
            img.raycastTarget = false;
            img.preserveAspect = true;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            return img;
        }

        // ---------- PUBLIC STRUCTURED API ----------

        public Canvas GetCanvas() => _canvas;

        public void SetCoinRow(int current, int max)
        {
            _coinValue.text = max > 0 ? $"{current} / {max}" : $"{current}";
        }
        public void SetScoreRow(int score) { _scoreValue.text = score.ToString("N0"); }

        public void SetTimeRow(float current, float max)
        {
            if (max > 900) {
                _timeValue.text = $"{current:0.0}s";
                _timeFill.fillAmount = 1f;
                _timeFill.sprite = Resources.Load<Sprite>("Sprites/UI/bar_fill_red");
                return;
            }
            _timeValue.text = $"{current:0.0}s / {max:0}s";
            float k = Mathf.Clamp01(current / max);
            _timeFill.fillAmount = k;
            // Color flip to red when < 20% time left
            float remaining = 1f - k;
            _timeFill.sprite = Resources.Load<Sprite>(remaining < 0.2f ? "Sprites/UI/bar_fill_red" : "Sprites/UI/bar_fill");
        }

        public void SetLevelBadge(int n) { _levelNumber.text = n.ToString(); }

        public void SetComboTag(int count, int mult)
        {
            if (count < 3) _comboTagline.text = "";
            else _comboTagline.text = $"COMBO  x{mult}  ({count})";
        }

        // Legacy stubs — GameRoot no longer calls these but keep for back-compat
        public void SetLeft(string s) { }
        public void SetRight(string s) { }
        public void SetBottom(string s) { if (_bottomText != null) _bottomText.text = s; }

        public void SetCenter(string s, float durationMs = 0)
        {
            _centerText.text = s;
            _centerUntil = durationMs > 0 ? Time.unscaledTime + durationMs / 1000f : 0;
        }

        public void UpdateScrubBar(float offsetSec, bool allowed, bool scrubHeld)
        {
            if (_scrubBar == null) return;
            _scrubBar.SetActive(allowed);
            if (!allowed) return;
            float norm = Mathf.Clamp(offsetSec / 2f, -1f, 0.4f);
            _scrubPlayhead.anchoredPosition = new Vector2(norm * 240f, 0);
            float alpha = scrubHeld ? 1f : 0.45f;
            _scrubPlayheadImg.color = new Color(0.4f, 0.75f, 1f, alpha);
            if (Mathf.Abs(offsetSec) < 0.05f) _scrubLabel.text = scrubHeld ? "HOLD Z + MOVE MOUSE" : "RUNTIME (hold Z to scrub)";
            else if (offsetSec < 0) _scrubLabel.text = $"REWIND  {offsetSec:0.0}s";
            else _scrubLabel.text = $"FORWARD  +{offsetSec:0.0}s";
        }

        public void UpdateComboCenter(int count, int mult)
        {
            if (_comboText == null) {
                _comboText = MakeText(_canvas.transform, "", 92,
                                       new Color(1f, 0.82f, 0.28f, 0f),
                                       new Vector2(0, 0), TextAnchor.MiddleCenter, 500, 140);
                var rt = _comboText.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(260, 100);
            }
            if (count < 3) {
                var c = _comboText.color; c.a = Mathf.MoveTowards(c.a, 0f, Time.unscaledDeltaTime * 3f); _comboText.color = c;
                return;
            }
            _comboText.text = $"x{mult}\n<size=36>{count} COMBO</size>";
            var col = _comboText.color; col.a = 1f; _comboText.color = col;
            if (count != _lastCombo) {
                _comboText.transform.localScale = Vector3.one * 1.35f;
                _lastCombo = count;
            } else {
                _comboText.transform.localScale = Vector3.MoveTowards(_comboText.transform.localScale, Vector3.one, Time.unscaledDeltaTime * 3f);
            }
        }

        public void BumpCoinCounter() { _coinPunchT = 0.25f; }

        void LateUpdate()
        {
            if (_coinPunchT > 0 && _coinValue != null) {
                _coinPunchT -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(_coinPunchT / 0.25f);
                float s = 1f + k * 0.25f;
                _coinValue.transform.localScale = new Vector3(s, s, 1f);
                if (_coinPunchT <= 0) _coinValue.transform.localScale = Vector3.one;
            }
        }

        public void ShowOverlay(string title, List<(string label, Action action)> buttons, string hint)
        {
            _overlayTitle.text = title;
            _overlayHint.text = hint ?? "";
            foreach (Transform c in _overlayButtons) Destroy(c.gameObject);
            foreach (var (label, action) in buttons) {
                var bgo = new GameObject("Btn_" + label);
                bgo.transform.SetParent(_overlayButtons, false);
                // Solid gold background — no sprite tinting (dark-on-dark invisibility bug)
                var img = bgo.AddComponent<Image>();
                img.sprite = null;
                img.color = new Color(0.96f, 0.82f, 0.34f, 1f);
                var btn = bgo.AddComponent<Button>();
                btn.targetGraphic = img;
                // Hover/pressed tints so button feels interactive
                var colors = btn.colors;
                colors.normalColor      = new Color(0.96f, 0.82f, 0.34f, 1f);
                colors.highlightedColor = new Color(1f,    0.92f, 0.55f, 1f);
                colors.pressedColor     = new Color(0.75f, 0.58f, 0.2f,  1f);
                colors.selectedColor    = new Color(0.96f, 0.82f, 0.34f, 1f);
                btn.colors = colors;
                var le = bgo.AddComponent<LayoutElement>();
                le.preferredHeight = 64; le.preferredWidth = 520;
                btn.onClick.AddListener(() => action?.Invoke());

                // Dark border / shadow via a child image beneath
                var shadowGo = new GameObject("shadow");
                shadowGo.transform.SetParent(bgo.transform, false);
                shadowGo.transform.SetAsFirstSibling();
                var shImg = shadowGo.AddComponent<Image>();
                shImg.color = new Color(0, 0, 0, 0.6f);
                shImg.raycastTarget = false;
                var shRT = shadowGo.GetComponent<RectTransform>();
                shRT.anchorMin = Vector2.zero; shRT.anchorMax = Vector2.one;
                shRT.offsetMin = new Vector2(4, -4); shRT.offsetMax = new Vector2(4, -4);

                // Button label — dark text on gold (proper contrast)
                var txtGo = new GameObject("L");
                txtGo.transform.SetParent(bgo.transform, false);
                var txt = txtGo.AddComponent<Text>();
                txt.font = _font;
                txt.fontSize = 22;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = new Color(0.12f, 0.08f, 0.02f, 1f);
                txt.text = label;
                txt.raycastTarget = false;
                var trt = txtGo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            }
            _overlay.SetActive(true);
        }

        public void HideOverlay() { _overlay.SetActive(false); }

        void Update()
        {
            if (_centerUntil > 0 && Time.unscaledTime > _centerUntil) {
                _centerText.text = ""; _centerUntil = 0;
            }
        }
    }
}
