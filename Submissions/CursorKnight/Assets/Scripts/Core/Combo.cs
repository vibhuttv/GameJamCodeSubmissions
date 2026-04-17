using UnityEngine;

namespace CursorSamurai.Core
{
    // Simple combo tracker: increments on coin/kill/obstacle-pass, resets on hit
    // or timeout. Score multiplier scales with combo count. Downwell pattern.
    public class Combo
    {
        public int Count;
        public float TimeLeft;   // resets to ComboWindow on any event
        public int Best;
        public const float ComboWindow = 4.0f;

        public int Multiplier {
            get {
                if (Count < 3)  return 1;
                if (Count < 8)  return 2;
                if (Count < 16) return 3;
                if (Count < 30) return 4;
                return 5;
            }
        }

        public void Bump()
        {
            Count++;
            TimeLeft = ComboWindow;
            if (Count > Best) Best = Count;
        }

        public void Break()
        {
            Count = 0;
            TimeLeft = 0;
        }

        public void Tick(float dt)
        {
            if (TimeLeft > 0) {
                TimeLeft -= dt;
                if (TimeLeft <= 0) Count = 0;
            }
        }

        public void Reset() { Count = 0; Best = 0; TimeLeft = 0; }
    }
}
