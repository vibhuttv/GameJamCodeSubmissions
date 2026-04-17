using UnityEngine;
using CursorSamurai.Core;

namespace CursorSamurai.Entities
{
    public class Boss : MonoBehaviour
    {
        public const int MaxHp = 7;           // 5 → 7 HP; finale feels earned
        public int HP = MaxHp;
        public bool Dead;

        const float AttackInterval = 2.4f;    // attacks more frequently
        const float WindUp = 0.45f;
        const float Active = 0.4f;
        const float Cooldown = 0.35f;

        float _attackT;
        int _phase = -1;
        float _flashT;
        SpriteRenderer _sr;

        public static Boss Build()
        {
            var go = new GameObject("Boss");
            var b = go.AddComponent<Boss>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteCache.Get("Sprites/boss");
            sr.sortingOrder = 25;
            b._sr = sr;
            return b;
        }

        public bool Tick(float dt, float samuraiY)
        {
            if (Dead) return false;
            _attackT += dt;
            bool didHit = false;
            if (_phase == -1 && _attackT >= AttackInterval) { _phase = 0; _attackT = 0; }
            if (_phase == 0) {
                transform.localScale = Vector3.one * (1f + _attackT / WindUp * 0.2f);
                if (_attackT >= WindUp) { _phase = 1; _attackT = 0; }
            } else if (_phase == 1) {
                // During active attack phase, samurai in center vertical lane (abs Y < 1) takes hit
                if (Mathf.Abs(samuraiY) < 1f) didHit = true;
                if (_attackT >= Active) { _phase = 2; _attackT = 0; }
            } else if (_phase == 2) {
                transform.localScale = Vector3.one * (1.2f - _attackT / Cooldown * 0.2f);
                if (_attackT >= Cooldown) { _phase = -1; _attackT = 0; transform.localScale = Vector3.one; }
            }
            if (_flashT > 0) { _flashT -= dt; if (_sr != null) _sr.color = Color.Lerp(Color.white, new Color(2f, 2f, 2f), _flashT * 5f); }
            else if (_sr != null) _sr.color = Color.white;
            return didHit && _phase == 1;
        }

        public bool Damage()
        {
            HP--;
            _flashT = 0.18f;
            AudioSystem.I?.PlaySfx("boss_hit");
            ScreenShake.I?.AddTrauma(0.45f);
            Hitstop.I?.Freeze(3);
            FlashFX.HurtFlash(_sr, 3);
            if (HP <= 0) {
                Dead = true;
                ScreenShake.I?.AddTrauma(1f);
                Hitstop.I?.Freeze(10);
                // Big screen flash on boss kill — sell the moment
                FlashFX.I?.FullScreenFlash(new Color(1f, 0.95f, 0.85f, 1f), 0.4f);
                return true;
            }
            return false;
        }

        public Bounds GetHitbox() => new Bounds(transform.position, new Vector3(1.8f, 2.4f, 1f));
    }
}
