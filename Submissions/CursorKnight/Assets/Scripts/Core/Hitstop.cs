using System.Collections;
using UnityEngine;

namespace CursorSamurai.Core
{
    // Celeste/Hollow Knight pattern — freeze time for N frames on impact.
    // The single biggest reason their games feel like meat and ours like plastic.
    public class Hitstop : MonoBehaviour
    {
        public static Hitstop I;
        Coroutine _running;

        void Awake() { I = this; }

        // Freeze for roughly `frames` frames at 60fps. Game SFX/UI keep playing
        // via unscaledDeltaTime; only gameplay Time.deltaTime halts.
        // Guards: never stack (ignore while active), never fire during Timeline Scrub
        // (would feel broken since scrub is already a time effect).
        public void Freeze(int frames = 4)
        {
            if (_running != null) return;
            if (CursorSamurai.GameRoot.I != null && CursorSamurai.GameRoot.I.CurrentScrubActive) return;
            _running = StartCoroutine(FreezeCo(frames / 60f));
        }

        IEnumerator FreezeCo(float seconds)
        {
            float prev = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(seconds);
            Time.timeScale = prev > 0 ? prev : 1f;
            _running = null;
        }
    }
}
