using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CursorSamurai.Core
{
    // Simple fade-to-black + level-name card for polished transitions between
    // Forest → Water → Cave. Uses unscaled time so a hitstop freeze doesn't kill it.
    public class LevelTransition : MonoBehaviour
    {
        Image _fade;
        RectTransform _card;
        Text _cardText;
        Font _font;

        public void Init(Canvas canvas, Font font)
        {
            _font = font;

            var fadeGo = new GameObject("Fade");
            fadeGo.transform.SetParent(canvas.transform, false);
            _fade = fadeGo.AddComponent<Image>();
            _fade.color = new Color(0, 0, 0, 0);
            _fade.raycastTarget = false;
            var fRT = fadeGo.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;
            fadeGo.transform.SetAsLastSibling();

            var cardGo = new GameObject("LevelCard");
            cardGo.transform.SetParent(canvas.transform, false);
            _card = cardGo.AddComponent<RectTransform>();
            _card.anchorMin = new Vector2(0.5f, 0.5f);
            _card.anchorMax = new Vector2(0.5f, 0.5f);
            _card.pivot = new Vector2(0.5f, 0.5f);
            _card.sizeDelta = new Vector2(900, 180);
            _card.anchoredPosition = new Vector2(2000, 0);

            var bg = cardGo.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.03f, 0.08f, 0.92f);
            bg.raycastTarget = false;

            var tGo = new GameObject("T");
            tGo.transform.SetParent(cardGo.transform, false);
            _cardText = tGo.AddComponent<Text>();
            _cardText.font = _font;
            _cardText.fontSize = 72;
            _cardText.alignment = TextAnchor.MiddleCenter;
            _cardText.color = new Color(1f, 0.88f, 0.45f);
            _cardText.raycastTarget = false;
            var tRT = tGo.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
        }

        public IEnumerator Run(string levelName, Action onMidpoint)
        {
            // Fade out
            yield return Fade(0f, 1f, 0.25f);
            onMidpoint?.Invoke();   // swap level here — black screen hides it
            // Slide card in, hold, slide out
            _card.anchoredPosition = new Vector2(2000, 0);
            _cardText.text = levelName;
            yield return SlideCard(2000f, 0f, 0.3f);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return SlideCard(0f, -2000f, 0.3f);
            // Fade back in
            yield return Fade(1f, 0f, 0.35f);
        }

        IEnumerator Fade(float from, float to, float dur)
        {
            float t = 0;
            while (t < dur) {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                var c = _fade.color; c.a = Mathf.Lerp(from, to, k); _fade.color = c;
                yield return null;
            }
            var final = _fade.color; final.a = to; _fade.color = final;
        }

        IEnumerator SlideCard(float fromX, float toX, float dur)
        {
            float t = 0;
            while (t < dur) {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                // Ease out-cubic
                k = 1 - Mathf.Pow(1 - k, 3);
                _card.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, k), 0);
                yield return null;
            }
            _card.anchoredPosition = new Vector2(toX, 0);
        }

        // Instant black fade from 0→1 (used when entering main menu)
        public IEnumerator FlashBlack() { yield return Fade(0f, 0.6f, 0.2f); yield return Fade(0.6f, 0f, 0.2f); }
    }
}
