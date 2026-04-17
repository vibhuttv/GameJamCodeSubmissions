using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CursorSamurai.Core;
using CursorSamurai.Entities;

namespace CursorSamurai.Levels
{
    // NOTE: class name is historical — this is now the FINAL level (Level 3 in UI).
    // The striderzz ice-cave palette gives it a proper "final boss arena" feel.
    // Boss logic (ported from the old forest level) triggers ~35s in.
    public class Level1Cave : RunnerLevel
    {
        readonly List<Enemy> _enemies = new List<Enemy>();
        float _enemyCooldown = 2.8f;
        Boss _boss;
        bool _bossTriggered, _bossDefeated;
        float _slowMoTimer;
        const float BossAtSec = 45f;   // more pre-boss runway for enemy chain

        System.Action<Vector2> _clickHandler;

        protected override void Configure()
        {
            LevelFolder = "Cave";
            SkyTopColor = new Color(0.11f, 0.15f, 0.35f);
            SkyBotColor = new Color(0.16f, 0.22f, 0.42f);
            GoalTimeSec = 9999f;        // ends via boss kill
            GoalCrystals = 0;
            SpawnInterval = (1.0f, 1.8f);
            CrystalInterval = (1.8f, 3.4f);    // fewer coins on finale — focus is combat
            AmbientTrack = "amb_cave";
        }

        public override void Begin()
        {
            AllowScrub = false;    // aim-X conflicts with scrub-X on this level
            base.Begin();
            _enemies.Clear();
            _enemyCooldown = 2.8f;
            _boss = null;
            _bossTriggered = false;
            _bossDefeated = false;
            _slowMoTimer = 0f;
            _clickHandler = ndc => OnClickTarget(ndc);
            Input.OnClickTarget += _clickHandler;
        }

        public override void End()
        {
            if (_clickHandler != null) Input.OnClickTarget -= _clickHandler;
            foreach (var e in _enemies) if (e != null) Destroy(e.gameObject);
            _enemies.Clear();
            if (_boss != null) Destroy(_boss.gameObject);
            _boss = null;
            base.End();
        }

        protected override Obstacle SpawnObstacle()
        {
            float r = Random.value;
            CaveKind k;
            if      (r < 0.20f) k = CaveKind.Drum;
            else if (r < 0.38f) k = CaveKind.SpikeBed;
            else if (r < 0.54f) k = CaveKind.Pillar;
            else if (r < 0.70f) k = CaveKind.Beam;
            else if (r < 0.88f) k = CaveKind.DrumStack;
            else                k = CaveKind.DrumGate;
            return Obstacle.BuildCave(k);
        }

        protected override void CustomTick(float dt)
        {
            float scale = _slowMoTimer > 0 ? 0.3f : 1f;
            if (_slowMoTimer > 0) _slowMoTimer -= dt;
            float bdt = dt * scale;

            if (!_bossTriggered && WorldTime >= BossAtSec) {
                _bossTriggered = true;
                _boss = Boss.Build();
                _boss.transform.SetParent(transform, false);
                _boss.transform.position = new Vector3(5f, 0.5f, 0);
                GameRoot.I.HUD.SetCenter("FINAL BOSS", 2000);
                SpawnInterval = (99f, 99f);
            }

            if (!_bossTriggered) {
                _enemyCooldown -= dt;
                if (_enemyCooldown <= 0f) {
                    var e = Enemy.Build();
                    e.transform.SetParent(transform, false);
                    e.transform.position = new Vector3(SpawnX, Random.Range(-2f, 1.5f), 0);
                    _enemies.Add(e);
                    _enemyCooldown = Random.Range(1.8f, 3.0f);   // more enemies pre-boss
                }
            }

            var hb = Samurai.GetHitbox();
            for (int i = _enemies.Count - 1; i >= 0; i--) {
                var e = _enemies[i];
                if (e == null) { _enemies.RemoveAt(i); continue; }
                e.transform.position += new Vector3(-Speed * dt, 0, 0);
                if (e.Killed || e.transform.position.x < DespawnX) { Destroy(e.gameObject); _enemies.RemoveAt(i); continue; }
                if (hb.Intersects(e.GetHitbox())) { Destroy(e.gameObject); _enemies.RemoveAt(i); OnDeath("slain by a shadow"); return; }
            }

            if (_boss != null) {
                bool didHit = _boss.Tick(bdt, Samurai.transform.position.y);
                if (didHit) OnDeath("crushed by the boss");
            }
        }

        void OnClickTarget(Vector2 ndc)
        {
            if (!Samurai.Alive) return;
            Samurai.TriggerSlash();
            float aimY = ndc.y * 3f;
            for (int i = 0; i < _enemies.Count; i++) {
                var e = _enemies[i];
                if (e == null || e.Killed) continue;
                float dy = e.transform.position.y - aimY;
                float dx = e.transform.position.x;
                if (Mathf.Abs(dy) < 1.2f && dx > -3 && dx < 8) {
                    e.Killed = true;
                    Score.AddEnemyKill();
                    AudioSystem.I?.PlaySfx("slash");
                    Hitstop.I?.Freeze(4);   // satisfying meat-impact freeze on slash connect
                    ScreenShake.I?.AddTrauma(0.2f);
                    DustPuff.Spawn(e.transform.position, new Color(0.85f, 0.05f, 0.1f, 0.8f), 5, spread: 0.4f);
                    return;
                }
            }
            if (_boss != null && !_boss.Dead) {
                if (_boss.GetHitbox().Contains(new Vector3(ndc.x * 6, aimY, 0))
                    || Mathf.Abs(_boss.transform.position.x - ndc.x * 6) < 2f) {
                    bool killed = _boss.Damage();
                    if (killed) { _slowMoTimer = 1.2f; Score.DefeatBoss(); GameRoot.I.StartCoroutine(DelayedVictory()); }
                }
            }
        }

        IEnumerator DelayedVictory() { yield return new WaitForSeconds(1.3f); OnVictory(); }
    }
}
