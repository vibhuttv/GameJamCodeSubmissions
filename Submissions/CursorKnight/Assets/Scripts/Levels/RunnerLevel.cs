using System.Collections.Generic;
using UnityEngine;
using CursorSamurai.Core;
using CursorSamurai.Entities;

namespace CursorSamurai.Levels
{
    // 2D infinite runner base. Side-scrolling with parallax.
    //   World scrolls along -X (obstacles spawn at right, move left past samurai).
    //   Samurai stays at x = -4 and steers y via cursor.
    public abstract partial class RunnerLevel : MonoBehaviour
    {
        protected const float SpawnX = 14f;
        protected const float DespawnX = -10f;
        protected const float BaseSpeed = 8f;

        public Samurai Samurai;
        public InputManager Input;
        public Score Score;

        protected string LevelFolder;     // "Cave" / "Water" / "Forest"
        protected float GoalTimeSec = 60f;
        protected int   GoalCrystals = 0;
        protected (float min, float max) SpawnInterval = (1.0f, 2.2f);
        protected (float min, float max) CrystalInterval = (0f, 0f);
        protected string AmbientTrack = "amb_cave";
        protected Color SkyTopColor = Color.black;
        protected Color SkyBotColor = Color.black;

        protected readonly List<Obstacle> Obstacles = new List<Obstacle>();
        protected readonly List<Crystal> Crystals = new List<Crystal>();
        protected readonly Dictionary<Transform, TimelineChannel> XChannels = new Dictionary<Transform, TimelineChannel>();
        public TimelineScrub Scrub = new TimelineScrub();
        // Boss level disables Scrub — click-aim conflicts with cursor X semantics.
        public bool AllowScrub = true;

        protected float WorldTime;
        protected float Speed;
        protected float SpawnCooldown;
        protected float CrystalCooldown;
        protected bool HitThisLevel;
        protected bool Ended;

        // Parallax layers
        Transform[] _bgLayerA = new Transform[3];
        Transform[] _bgLayerB = new Transform[3];
        readonly float[] _bgSpeedMul = { 0.1f, 0.35f, 0.7f };
        float _bgWidth = 20f; // computed from actual BG sprite in BuildWorld

        Transform _ground;
        float _groundScrollU;

        public virtual void Begin()
        {
            Configure();
            Scrub.Reset();
            WorldTime = 0;
            Speed = BaseSpeed;
            SpawnCooldown = 1.0f;
            CrystalCooldown = 1.5f;
            HitThisLevel = false;
            Ended = false;

            BuildWorld();
            Samurai.Reset();
            Samurai.EnsureShadow(Samurai.GroundTopY);   // anchor visual — biggest "floating" killer
            AudioSystem.I?.PlayMusic(AmbientTrack);
            ScreenShake.I?.ResetShake();
            SpawnAmbientEmitters();
            if (Camera.main != null) Camera.main.backgroundColor = SkyTopColor;
        }

        public virtual void End()
        {
            AudioSystem.I?.StopMusic();
            foreach (var ob in Obstacles) if (ob != null) Destroy(ob.gameObject);
            foreach (var cr in Crystals) if (cr != null) Destroy(cr.gameObject);
            Obstacles.Clear(); Crystals.Clear(); XChannels.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            Ended = true;
        }

        protected abstract void Configure();
        protected abstract Obstacle SpawnObstacle();

        void BuildWorld()
        {
            // Compute natural BG width from the "far" sprite so we don't stretch
            var farSprite = SpriteCache.Get($"Sprites/BG/{LevelFolder}/far");
            if (farSprite != null) {
                float cam = Camera.main != null ? Camera.main.orthographicSize * 2f : 9f;
                float srcH = farSprite.rect.height / farSprite.pixelsPerUnit;
                float fitScale = cam / srcH;
                float srcW = farSprite.rect.width / farSprite.pixelsPerUnit;
                _bgWidth = srcW * fitScale - 0.1f; // slight negative overlap for seamless scroll
            }

            // Parallax BG layers (duplicated for seamless scroll)
            string[] names = { "far", "mid", "near" };
            float[] zDepths = { 10f, 5f, 2f };
            for (int i = 0; i < 3; i++) {
                _bgLayerA[i] = MakeBgSprite($"Sprites/BG/{LevelFolder}/{names[i]}", 0, zDepths[i], -100 + i * 5);
                _bgLayerB[i] = MakeBgSprite($"Sprites/BG/{LevelFolder}/{names[i]}", _bgWidth, zDepths[i], -100 + i * 5);
            }

            // Ground
            var ground = new GameObject("Ground");
            ground.transform.SetParent(transform, false);
            var sr = ground.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteCache.Get("Sprites/ground_" + LevelFolder.ToLower());
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(60f, 1.6f);
            sr.sortingOrder = 10;
            ground.transform.position = new Vector3(0, -3.2f, 0);
            _ground = ground.transform;
        }

        Transform MakeBgSprite(string path, float x, float zDepth, int order)
        {
            var go = new GameObject("BG_" + path);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteCache.Get(path);
            sr.sortingOrder = order;
            sr.drawMode = SpriteDrawMode.Simple;
            // Keep natural aspect ratio — scale uniformly to fit camera height
            var s = sr.sprite;
            if (s != null) {
                float cam = Camera.main != null ? Camera.main.orthographicSize * 2f : 9f;
                float srcH = s.rect.height / s.pixelsPerUnit;
                float fit = cam / srcH;
                go.transform.localScale = new Vector3(fit, fit, 1f);
            }
            // Value compression by depth — far = 45%, mid = 70%, near = 100%.
            // Also tint far layers toward the level fog color for atmospheric feel.
            float bright = path.Contains("/far")  ? 0.45f
                         : path.Contains("/mid")  ? 0.72f
                         : 1f;
            Color tint = path.Contains("/far")
                ? Color.Lerp(SkyTopColor, Color.white, 0.35f)
                : path.Contains("/mid")
                    ? Color.Lerp(SkyTopColor, Color.white, 0.65f)
                    : Color.white;
            sr.color = new Color(tint.r * bright, tint.g * bright, tint.b * bright, 1f);
            go.transform.position = new Vector3(x, 0f, zDepth);
            return go.transform;
        }

        public virtual void Tick(float dt)
        {
            if (Ended) return;
            WorldTime += dt;
            // Aggressive speed ramp: starts at 8, peaks at 20 over ~80 seconds
            Speed = BaseSpeed + Mathf.Min(12f, WorldTime / 7f);

            Scrub.Update(dt, AllowScrub ? Input.ScrubX : 0f);
            float scrubOffset = Scrub.OffsetSeconds;

            Samurai.Tick(dt);

            // Advance obstacles + crystals (-X scroll)
            foreach (var ob in Obstacles) {
                if (ob == null) continue;
                ob.transform.position += new Vector3(-Speed * dt, 0, 0);
                RecordX(ob.transform);
            }
            foreach (var cr in Crystals) {
                if (cr == null) continue;
                cr.transform.position += new Vector3(-Speed * dt, 0, 0);
                RecordX(cr.transform);
            }

            // Apply scrub
            if (Mathf.Abs(scrubOffset) > 0.01f) {
                float at = WorldTime + scrubOffset;
                foreach (var ob in Obstacles) if (ob != null) ApplyScrub(ob.transform, at);
                foreach (var cr in Crystals) if (cr != null) ApplyScrub(cr.transform, at);
            }

            // Parallax scroll
            for (int i = 0; i < 3; i++) {
                float spd = Speed * _bgSpeedMul[i] * dt;
                _bgLayerA[i].position += new Vector3(-spd, 0, 0);
                _bgLayerB[i].position += new Vector3(-spd, 0, 0);
                if (_bgLayerA[i].position.x < -_bgWidth) _bgLayerA[i].position += new Vector3(_bgWidth * 2, 0, 0);
                if (_bgLayerB[i].position.x < -_bgWidth) _bgLayerB[i].position += new Vector3(_bgWidth * 2, 0, 0);
            }

            // Ground — simulate scroll via x offset of the tiled SpriteRenderer
            if (_ground != null) {
                var sr = _ground.GetComponent<SpriteRenderer>();
                // Tiled draw mode uses the material's offset; but easier: shift transform then snap
                _ground.position += new Vector3(-Speed * dt, 0, 0);
                if (_ground.position.x < -10f) _ground.position += new Vector3(10f, 0, 0);
            }

            // Spawn
            SpawnCooldown -= dt;
            if (SpawnCooldown <= 0f) {
                var ob = SpawnObstacle();
                if (ob != null) {
                    ob.transform.SetParent(transform, false);
                    float y;
                    if (ob.Floating) {
                        y = Samurai.GroundTopY + 1.5f + Random.Range(-0.2f, 0.4f);
                    } else {
                        y = Samurai.GroundTopY + ob.SpriteHalfHeight;
                    }
                    ob.transform.position = new Vector3(SpawnX, y, 0);
                    Obstacles.Add(ob);

                    // Random vertical oscillation for some floating obstacles after t=10s
                    if (ob.Floating && WorldTime > 10f && Random.value < 0.35f) {
                        ob.gameObject.AddComponent<OscillateY>().Setup(
                            ob.transform.position.y,
                            Random.Range(0.4f, 0.9f),
                            Random.Range(1.0f, 2.0f));
                    }
                }
                // Cluster pattern after t=12s: 22% chance to spawn a follow-up in 0.3s
                bool cluster = WorldTime > 12f && Random.value < 0.22f;
                if (cluster) {
                    SpawnCooldown = 0.3f;
                } else {
                    SpawnCooldown = Random.Range(SpawnInterval.min, SpawnInterval.max)
                                  * Mathf.Max(0.45f, 1f - WorldTime / 80f);   // tightens 2x over 80s
                }
            }
            if (CrystalInterval.max > 0) {
                CrystalCooldown -= dt;
                if (CrystalCooldown <= 0f) {
                    var cr = Crystal.Build();
                    cr.transform.SetParent(transform, false);
                    // Crystals float in mid-air within samurai's vertical steering zone
                    float cy = Samurai.GroundTopY + Random.Range(0.8f, 3.6f);
                    cr.SetBasePos(new Vector3(SpawnX, cy, 0));
                    Crystals.Add(cr);
                    CrystalCooldown = Random.Range(CrystalInterval.min, CrystalInterval.max);
                }
            }

            // Despawn
            for (int i = Obstacles.Count - 1; i >= 0; i--) {
                var ob = Obstacles[i];
                if (ob == null || ob.transform.position.x < DespawnX) { if (ob != null) Destroy(ob.gameObject); Obstacles.RemoveAt(i); }
            }
            for (int i = Crystals.Count - 1; i >= 0; i--) {
                var cr = Crystals[i];
                if (cr == null || cr.Collected || cr.transform.position.x < DespawnX) { if (cr != null) Destroy(cr.gameObject); Crystals.RemoveAt(i); }
            }

            // Collisions
            var hb = Samurai.GetHitbox();
            foreach (var ob in Obstacles) {
                if (ob == null) continue;
                if (ob.Intersects(hb)) { OnDeath(ob.Fatal ? "fell / fatal hazard" : "hit an obstacle"); return; }
            }
            foreach (var cr in Crystals) {
                if (cr == null || cr.Collected) continue;
                if (cr.GetHitbox().Intersects(hb)) {
                    cr.Collected = true;
                    Score.AddCrystal();
                    AudioSystem.I?.PlaySfx("crystal");
                    // No hitstop on coin — feels laggy when player streaks them.
                    DustPuff.Spawn(cr.transform.position, new Color(1f, 0.85f, 0.3f, 0.9f), 5, spread: 0.3f);
                    GameRoot.I.HUD.BumpCoinCounter();
                }
            }

            Score.Tick(dt);
            CustomTick(dt);

            if (GoalTimeSec > 0 && WorldTime >= GoalTimeSec) {
                if (GoalCrystals <= 0 || Score.Crystals >= GoalCrystals) OnVictory();
            }
        }

        protected virtual void CustomTick(float dt) { }

        void SpawnAmbientEmitters()
        {
            var kind = LevelFolder == "Forest" ? AmbientParticles.Kind.ForestLeaves
                     : LevelFolder == "Water"  ? AmbientParticles.Kind.WaterBubbles
                     :                           AmbientParticles.Kind.CaveSnow;
            AmbientParticles.Create(transform, kind);
        }

        protected virtual void OnDeath(string reason)
        {
            if (Ended) return;
            Ended = true;
            Samurai.Die();
            Score.BreakCombo();
            Score.RegisterDeath();
            Input.Frozen = true;
            // Desaturate the world via Post Processing — shifts emotional register to "lost"
            StartCoroutine(DesaturateOnDeath());
            GameRoot.I.ShowDeathOverlay(reason, this);
        }

        System.Collections.IEnumerator DesaturateOnDeath()
        {
            float t = 0; float dur = 0.6f;
            while (t < dur) {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                // Use PP's color grading saturation — lerp toward -100 (gray)
                if (GameRoot.I.PP != null) {
                    // reuse its FeedScrub path with an artificial "rewind" effect? Instead call SetLevelTint.
                    // Simpler: tint colorFilter toward dark red-grey
                    GameRoot.I.PP.SetLevelTint(Color.Lerp(Color.white, new Color(0.6f, 0.5f, 0.5f), k));
                }
                yield return null;
            }
        }

        protected virtual void OnVictory()
        {
            if (Ended) return;
            Ended = true;
            Score.CompleteLevel(!HitThisLevel);
            Input.Frozen = true;
            StartCoroutine(VictoryBeat());
        }

        // Brief slow-mo + beat before level transition so the moment feels deliberate
        System.Collections.IEnumerator VictoryBeat()
        {
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(0.45f);
            Time.timeScale = 1f;
            GameRoot.I.OnLevelVictory(this);
        }

        void RecordX(Transform t)
        {
            if (!XChannels.TryGetValue(t, out var ch)) { ch = new TimelineChannel(); XChannels[t] = ch; }
            ch.Push(WorldTime, t.position.x);
        }
        void ApplyScrub(Transform t, float at)
        {
            if (!XChannels.TryGetValue(t, out var ch)) return;
            if (ch.TrySample(at, out float x)) { var p = t.position; p.x = x; t.position = p; }
        }
    }

    public partial class RunnerLevel
    {
        // UI-level ordering: Forest shows as 1, Water as 2, Cave (boss) as 3.
        public int LevelNumber => this is Level3Forest ? 1 : this is Level2Water ? 2 : this is Level1Cave ? 3 : 0;
        public float WorldTimePublic => WorldTime;
        public float GoalTimeSecPublic => GoalTimeSec;
        public int GoalCrystalsPublic => GoalCrystals;
    }
}
