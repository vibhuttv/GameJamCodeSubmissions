using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CursorSamurai.Core
{
    // Visual punctuation: sprite hurt-flash (white) + full-screen flash overlay
    // for boss kill / level complete / big moments.
    public class FlashFX : MonoBehaviour
    {
        public static FlashFX I;
        Image _screenFlash;

        void Awake() { I = this; }

        public void InitScreenFlash(Canvas canvas)
        {
            var go = new GameObject("ScreenFlash");
            go.transform.SetParent(canvas.transform, false);
            _screenFlash = go.AddComponent<Image>();
            _screenFlash.color = new Color(1, 1, 1, 0);
            _screenFlash.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            go.transform.SetAsLastSibling();
        }

        public void FullScreenFlash(Color c, float duration = 0.25f)
        {
            if (_screenFlash == null) return;
            StartCoroutine(FlashCo(c, duration));
        }

        IEnumerator FlashCo(Color c, float dur)
        {
            float t = 0;
            while (t < dur) {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                var col = c; col.a = (1f - k) * c.a; _screenFlash.color = col;
                yield return null;
            }
            _screenFlash.color = new Color(c.r, c.g, c.b, 0);
        }

        // Sprite hurt-flash: briefly tint any SpriteRenderer white.
        public static void HurtFlash(SpriteRenderer sr, int frames = 3)
        {
            if (sr == null) return;
            if (I != null) I.StartCoroutine(I.HurtFlashCo(sr, frames));
        }

        IEnumerator HurtFlashCo(SpriteRenderer sr, int frames)
        {
            Color orig = sr.color;
            sr.color = new Color(3f, 3f, 3f, orig.a);   // HDR-ish boost for bloom punch
            yield return new WaitForSecondsRealtime(frames / 60f);
            if (sr != null) sr.color = orig;
        }
    }
}
